using System.Transactions;
using Api.Data;
using Api.CustomException;
using Api.Models;
using Api.Services.Interfaces;
using Gremlin.Net.Process.Traversal;
using Api.Models.Data;
using Azure.Storage.Blobs;
using CsvHelper;
using System.Globalization;
using Newtonsoft.Json;
using CsvHelper.Configuration;
using Nest;
using System.Text;

public class TransferService : ITransferService
{
    IGraphService _graphService;
    private readonly ApplicationDbContext _context;

    private IQueuePublisherService _queuePublisherService;
    private readonly string? _blobConnectionString;
    private readonly string? _blobContainerName;
    private IConfiguration _configuration;
    private IElasticSearchService _ElasticSearchService;
    private ITransactionIngestGraphService _graphIngestService;
    private ElasticClient _Client;

    public TransferService(IGraphService graphService, ApplicationDbContext context,
    ITransactionIngestGraphService TransactionGraphService,
    IQueuePublisherService queuePublisherService, IConfiguration configuration,
    IElasticSearchService ElasticSearchService)
    {
        _graphService = graphService;
        _configuration = configuration;
        _graphIngestService = TransactionGraphService;
        _context = context;
        _queuePublisherService = queuePublisherService;
        _ElasticSearchService = ElasticSearchService;
        _blobConnectionString = _configuration.GetSection("AzureBlobStorage:ConnectionString").Value;
        _blobContainerName = _configuration.GetSection("AzureBlobStorage:ContainerName").Value;
    }
    private ElasticClient ElasticClient(int ObservatoryId, bool Refresh = false)
    {
        if (!Refresh && _Client != null)
        {
            return _Client;
        }
        var Observatory = _context.Observatories.Find(ObservatoryId);
        if (Observatory == null || Observatory.UseDefault)
        {
            _Client = _ElasticSearchService.connect();
            return _Client;
        }
        var Host = Observatory.ElasticSearchHost;
        if (!Observatory.UseDefault && Host == null)
        {
            throw new ValidateErrorException("Unable to connect to Elastic Search");
        }
        _Client = _ElasticSearchService.connect(Host);
        return _Client;

    }
    public async Task<TransactionDocument> Ingest(TransactionTransferRequest request, bool IndexToGraph = true)
    {
        ElasticClient(request.ObservatoryId);
        List<string> errors = ValidateTransactionTransfer(request);
        if (errors.Count > 0)
        {
            throw new ValidateErrorException(string.Join(", ", errors));
        }
        try
        {
            var d = GetTransaction(request.Transaction.TransactionId, request.ObservatoryId);
            if (d != null)
            {
                return d;
            }

            var DebitCustomer = AddCustomer(request.DebitCustomer);
            var DebitAccount = AddAccount(request.DebitCustomer.Account, DebitCustomer);

            var CreditCustomer = AddCustomer(request.CreditCustomer);
            var CreditAccount = AddAccount(request.CreditCustomer.Account, CreditCustomer);

            var transaction = AddTransaction(request, DebitAccount, CreditAccount);

            var TransactionData = new TransactionIngestData
            {
                DebitCustomer = DebitCustomer,
                DebitAccount = DebitAccount,
                CreditCustomer = CreditCustomer,
                CreditAccount = CreditAccount,
                Transaction = transaction,
                ObservatoryId = request.ObservatoryId
            };
            if (request.DebitCustomer.Device != null && request.DebitCustomer.Device?.DeviceId != null)
            {
                var TransactionProfile = await AddDevice(request.DebitCustomer.Device, DebitCustomer, transaction);
                TransactionData.Device = TransactionProfile;
                _Client.Update<TransactionDocument, object>(transaction.PlatformId, t => t.Doc(
                   new
                   {
                       DeviceDocumentId = TransactionProfile.DeviceId
                   }
                   ));
            }
            if (IndexToGraph)
            {
                var successfullyIndexed = await _graphIngestService.IngestTransactionInGraph(TransactionData);
            }

            return transaction;

        }
        catch (Exception Exception)
        {
            throw new ValidateErrorException("There were issues in completing the Transaction " + Exception.Message);
        }
    }
    private TransactionDocument? GetTransaction(string TransactionId, int observatoryId)
    {
        try
        {
            var CustomerRequest =
            _Client.Search<TransactionDocument>(c =>
            c.Size(1).Query(q => q.Bool(q => q.Must(
            sh => sh.Match(m => m.Field(f => f.TransactionId).Query(TransactionId))
            ))));

            if (CustomerRequest.Documents.Count > 0)
            {
                return CustomerRequest.Documents.First();
            }
            return null;
        }
        catch (Exception Exception)
        {
            throw new ValidateErrorException("There were issues in completing the Transaction " + Exception.Message);
        }
    }
    public async Task<bool> UploadAndIngest(int ObservatoryId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ValidateErrorException("No file was uploaded.");
        }

        if (_blobConnectionString == null || _blobContainerName == null)
        {
            throw new ValidateErrorException("Unable to initiate the blob");
        }
        try
        {
            var blobServiceClient = new BlobServiceClient(_blobConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_blobContainerName);
            await containerClient.CreateIfNotExistsAsync();
            var blobName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var blobClient = containerClient.GetBlobClient(blobName);
            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream);
            }
            var url = blobClient.Uri.ToString();
            var fileRequestUrl = new FileData
            {
                Url = url,
                Name = blobName,
                ObservatoryId = ObservatoryId
            };
            var requestString = JsonConvert.SerializeObject(fileRequestUrl);
            var IngestFileQueueName = _configuration.GetValue<string>("IngestFileQueueName");
            if (IngestFileQueueName == null)
            {
                throw new ValidateErrorException("Invalid Queue Name");
            }
            _queuePublisherService.Publish(IngestFileQueueName, requestString);

            return true;
        }
        catch (Exception ex)
        {
            throw new ValidateErrorException($"Internal server error: {ex.Message}");
        }
    }
    private TransactionTransferRequest MakeRequest(TransactionCsvRecord record)
    {
        var AccountRequest = new AccountRequest
        {
            AccountNumber = record.DebitCustomerAccountNumber,
            BankCode = record.DebitCustomerBankCode,
            Country = record.DebitCustomerCountry
        };
        var Device = new DeviceRequest
        {
            DeviceId = record.DebitCustomerDeviceId,
            DeviceType = (DeviceType?)record.DebitCustomerDeviceType,
            IpAddress = record.DebitCustomerIpAddress
        };
        var DebitCustomer = new CustomerRequest
        {
            Account = AccountRequest,
            Email = record.DebitCustomerEmail,
            Name = record.DebitCustomerName,
            Phone = record.DebitCustomerPhone,
            Device = Device
        };
        var CreditAccountRequest = new AccountRequest
        {
            AccountNumber = record.CreditCustomerAccountNumber,
            BankCode = record.CreditCustomerBankCode,
            Country = record.CreditCustomerCountry
        };
        var CreditCustomer = new CustomerRequest
        {
            Account = CreditAccountRequest,
            Email = record.CreditCustomerEmail,
            Name = record.CreditCustomerName,
            Phone = record.CreditCustomerPhone,
        };
        var Transaction = new TransactionRequest
        {
            Amount = (float)record.TransactionAmount,
            TransactionDate = record.TransactionDate,
            TransactionId = record.TransactionId,
            Description = record.TransactionDescription,

        };
        var request = new TransactionTransferRequest
        {
            DebitCustomer = DebitCustomer,
            CreditCustomer = CreditCustomer,
            Transaction = Transaction,
            ObservatoryId = record.ObservatoryId
        };
        return request;

    }
    private CustomerDocument AddCustomer(CustomerRequest customerRequest)
    {

        try
        {
            var CustomerRequest =
            _Client.Search<CustomerDocument>(c =>
            c.Size(1).Query(q => q.Bool(q => q.Must(
            sh => sh.Match(m => m.Field(f => f.Email).Query(customerRequest.Email)),
            sh => sh.Match(m => m.Field(f => f.Phone).Query(customerRequest.Phone))
            ))));

            if (CustomerRequest.Documents.Count <= 0)
            {
                var Document = new CustomerDocument
                {
                    CustomerId = Guid.NewGuid().ToString(),
                    FullName = customerRequest.Name,
                    Email = customerRequest.Email,
                    Phone = customerRequest.Phone,
                    CreatedAt = DateTime.Now,
                    Type = "Customer"
                };
                var response = _Client.IndexDocument(Document);
                return Document;
            }
            return CustomerRequest.Documents.First();
        }
        catch (Exception Exception)
        {
            throw new ValidateErrorException("There were issues in completing the Transaction " + Exception.Message);
        }
    }
    private AccountDocument AddAccount(AccountRequest accountRequest, CustomerDocument customer)
    {
        try
        {
            // move this to cache
            var Bank = _context.Banks.Where(b => b.Code == accountRequest.BankCode).First();
            var AccountRequest = _Client.Search<AccountDocument>(c =>
                c.Size(1).Query(q => q.Bool(q => q.Must(
                sh => sh.Match(m => m.Field(f => f.AccountNumber).Query(accountRequest.AccountNumber)),
                sh => sh.Term(m => m.Field(f => f.BankId).Value(Bank.Id))
                ))));

            if (AccountRequest.Documents.Count <= 0)
            {
                var Account = new AccountDocument
                {
                    AccountNumber = accountRequest.AccountNumber,
                    AccountBalance = accountRequest.Balance,
                    AccountId = Guid.NewGuid().ToString(),
                    BankId = Bank.Id,
                    AccountType = accountRequest.AccountType,
                    CustomerId = customer.CustomerId,
                    CreatedAt = DateTime.Now,
                    Type = "Account"
                };
                _Client.IndexDocument(Account);
                return Account;
            }
            return AccountRequest.Documents.First();
        }
        catch (Exception Exception)
        {
            throw new ValidateErrorException(Exception.Message);
        }
    }

    private TransactionDocument AddTransaction(TransactionTransferRequest request, AccountDocument debitAccount, AccountDocument creditAccount)
    {
        try
        {
            var transaction = new TransactionDocument
            {
                Amount = request.Transaction.Amount,
                ObservatoryId = request.ObservatoryId,
                PlatformId = Guid.NewGuid().ToString(),
                TransactionId = request.Transaction.TransactionId,
                CreditAccountId = creditAccount.AccountId,
                DebitAccountId = debitAccount.AccountId,
                CreatedAt = DateTime.Now,
                Currency = request.Transaction.Currency,
                Description = request.Transaction.Description,
                TransactionType = TransactionType.Transfer,
                TransactionDate = request.Transaction.TransactionDate.ToUniversalTime(),
                Type = "Transaction"
            };

            _Client.IndexDocument(transaction);
            return transaction;
        }
        catch (Exception Exception)
        {
            throw new ValidateErrorException("There were issues in completing the Transaction " + Exception.Message);
        }

    }
    private async Task<DeviceDocument> AddDevice(DeviceRequest request, CustomerDocument customer, TransactionDocument transaction)
    {
        try
        {
            var ProfileRequest =
           _Client.Search<DeviceDocument>(c =>
           c.Size(1).Query(q => q.Bool(q => q.Must(
           sh => sh.Match(m => m.Field(f => f.DeviceId).Query(request.DeviceId)),
           sh => sh.Match(m => m.Field(f => f.CustomerId).Query(customer.CustomerId))
           ))));
            if (ProfileRequest.Documents.Count <= 1)
            {
                var profile = new DeviceDocument
                {
                    CustomerId = customer.CustomerId,
                    ProfileId = Guid.NewGuid().ToString(),
                    DeviceId = request.DeviceId,
                    DeviceType = request.DeviceType,
                    IpAddress = request.IpAddress,
                    CreatedAt = DateTime.Now,
                    Type = "Device"
                };
                var response = _Client.IndexDocument(profile);
                return profile;
            }
            return ProfileRequest.Documents.First();
        }
        catch (Exception Exception)
        {
            throw new ValidateErrorException("There were issues in completing the Transaction " + Exception.Message);
        }
    }
    public List<string> ValidateTransactionTransfer(TransactionTransferRequest request)
    {
        var errors = new List<string>();
        var debitBank = _context.Banks.Where(b => b.Code == request.DebitCustomer.Account.BankCode && b.Country == request.DebitCustomer.Account.Country).FirstOrDefault();
        if (debitBank == null)
        {
            errors.Add("Invalid Debit Account Bank Supplied");
        }
        var creditBank = _context.Banks.Where(b => b.Code == request.CreditCustomer.Account.BankCode && b.Country == request.CreditCustomer.Account.Country).FirstOrDefault();
        if (creditBank == null)
        {
            errors.Add("Invalid Credit Account Bank Supplied");
        }
        if (request.ObservatoryId <= 0)
        {
            errors.Add("Please Specify what observatory you are monitoring");
        }

        return errors;
    }

    public async Task<bool> DownloadFileAndIngest(FileData data)
    {

        var blobServiceClient = new BlobServiceClient(_blobConnectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(_blobContainerName);
        BlobClient blobClient = containerClient.GetBlobClient(data.Name);
        using (var memoryStream = new MemoryStream())
        {
            await blobClient.DownloadToAsync(memoryStream);

            // Reset the stream position to the beginning
            memoryStream.Position = 0;

            using (var reader = new StreamReader(memoryStream))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            }))
            {
                var records = csv.GetRecords<TransactionCsvRecord>().ToList();

                // Process the CSV records
                foreach (var record in records)
                {
                    var request = MakeRequest(record);
                    var requestString = JsonConvert.SerializeObject(request);
                    var queueName = _configuration.GetValue<string>("IngestQueueName");
                    _queuePublisherService.Publish(queueName, requestString);
                    // await Ingest(request, true);
                }
                //   await _graphIngestService.RunAnalysis(data.ObservatoryId);
            }
        }
        return true;
    }
}