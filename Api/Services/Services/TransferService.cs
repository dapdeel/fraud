using System.Transactions;
using Api.Data;
using Api.CustomException;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
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
using Hangfire;

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
    private int maxSizeInBytes = 50 * 1024 * 1024;
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
    private ElasticClient ElasticClient(string ObservatoryTag, bool Refresh = false)
    {
        if (!Refresh && _Client != null)
        {
            return _Client;
        }

        // Filter using ObservatoryTag instead of Find by ID
        var Observatory = _context.Observatories.FirstOrDefault(o => o.ObservatoryTag == ObservatoryTag);

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
        ElasticClient(request.ObservatoryTag);
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
                ObservatoryTag = request.ObservatoryTag
            };
            if (request.DebitCustomer.Device != null && request.DebitCustomer.Device?.DeviceId != null)
            {
                var TransactionProfile = AddDevice(request.DebitCustomer.Device, DebitCustomer, transaction);
                TransactionData.Device = TransactionProfile;
                var transactionDetail = GetTransaction(transaction.PlatformId);
                if (transactionDetail != null)
                {
                    _Client.Update<TransactionDocument, object>(transactionDetail.Id, t => t.Doc(
                       new
                       {
                           DeviceDocumentId = TransactionProfile.DeviceId
                       }
                       ));
                }
            }
            if (IndexToGraph)
            {
                var successfullyIndexed = await _graphIngestService.IngestTransactionInGraph(TransactionData);
            }
            else
            {
                BackgroundJob.Enqueue(() => _graphIngestService.IngestTransactionInGraph(TransactionData));
            }

            return transaction;

        }
        catch (Exception Exception)
        {
            return null;
            // throw new ValidateErrorException("There were issues in completing the Transaction " + Exception.Message);
        }
    }
    private TransactionDocument? GetTransaction(string TransactionId, int observatoryId)
    {
        try
        {
            var CustomerRequest =
            _Client.Search<TransactionDocument>(c =>
            c.Query(q => q.Bool(
                b => b.Filter(f =>
                f.Bool(b => b.Should(sh => sh.MatchPhrase(mp => mp.Field(f => f.TransactionId).Query(TransactionId)))),
                f => f.Bool(b => b.Should(sh => sh.MatchPhrase(mp => mp.Field(f => f.ObservatoryId).Query(observatoryId.ToString()))))
                )
            )));

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
    private Nest.IHit<TransactionDocument>? GetTransaction(string PlatformId)
    {
        try
        {
            var CustomerRequest =
            _Client.Search<TransactionDocument>(c =>
            c.Query(q => q.Bool(
                b => b.Filter(f =>
                f.Bool(b => b.Should(sh => sh.MatchPhrase(mp => mp.Field(f => f.PlatformId).Query(PlatformId))))
                //      f => f.Bool(b => b.Should(sh => sh.MatchPhrase(mp => mp.Field(f => f.ObservatoryId).Query(observatoryId.ToString()))))
                )
            )));

            return CustomerRequest.Hits.FirstOrDefault();

        }
        catch (Exception Exception)
        {
            throw new ValidateErrorException("There were issues in completing the Transaction " + Exception.Message);
        }
    }

    public async Task<string> UploadAndIngest(string ObservatoryId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ValidateErrorException("No file was uploaded.");
        }
        if (file.Length > maxSizeInBytes)
        {
            throw new ValidateErrorException("The File Size is too much, Max of 50MB is Accepted");
        }

        var awsAccessKey = _configuration.GetValue<string>("AWS:AccessKey");
        var awsSecretKey = _configuration.GetValue<string>("AWS:SecretKey");
        var awsBucketName = _configuration.GetValue<string>("AWS:BucketName");
        var awsRegion = _configuration.GetValue<string>("AWS:Region");

        if (string.IsNullOrEmpty(awsAccessKey) || string.IsNullOrEmpty(awsSecretKey) || string.IsNullOrEmpty(awsBucketName) || string.IsNullOrEmpty(awsRegion))
        {
            throw new ValidateErrorException("AWS configuration is invalid.");
        }

        try
        {
            var regionEndpoint = Amazon.RegionEndpoint.GetBySystemName(awsRegion);
            var s3Client = new AmazonS3Client(awsAccessKey, awsSecretKey, regionEndpoint);
            var transferUtility = new TransferUtility(s3Client);

            var fileExtension = Path.GetExtension(file.FileName);
            var fileName = Guid.NewGuid().ToString() + fileExtension;

            using (var stream = file.OpenReadStream())
            {
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = stream,
                    Key = fileName,
                    BucketName = awsBucketName,
                    ContentType = file.ContentType,
                    CannedACL = S3CannedACL.Private
                };

                await transferUtility.UploadAsync(uploadRequest);
            }
            var url = $"https://{awsBucketName}.s3.{awsRegion}.amazonaws.com/{fileName}";
            var fileRequestUrl = new FileData
            {
                Url = url,
                Name = fileName,
                ObservatoryId = ObservatoryId
            };

            var transactionFileDocument = new Api.Models.TransactionFileDocument
            {
                Name = fileName,
                Url = url,
                ObservatoryId = ObservatoryId,
                Indexed = false
            };

            _context.Add(transactionFileDocument);
            var requestString = JsonConvert.SerializeObject(fileRequestUrl);

            var ingestFileQueueName = _configuration.GetValue<string>("IngestFileQueueName");
            if (string.IsNullOrEmpty(ingestFileQueueName))
            {
                throw new ValidateErrorException("Invalid Queue Name");
            }

            _queuePublisherService.PublishAsync(ingestFileQueueName, requestString);
            _context.SaveChanges();

            return url;
        }
        catch (AmazonS3Exception ex)
        {
            throw new ValidateErrorException($"Error encountered on server when uploading file to S3: {ex.Message}");
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
            ObservatoryTag= record.ObservatoryTag,
           ObservatoryId=record.ObservatoryId
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
                    Document = NodeData.Customer,
                    Type = DocumentType.Node
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
                    Type = DocumentType.Node,
                    Document = NodeData.Account
                };
                var response = _Client.IndexDocument(Account);
                if (!response.IsValid)
                {
                    throw new ValidateErrorException("Unable to create account");
                }
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
                ObservatoryTag = request.ObservatoryTag,
                PlatformId = Guid.NewGuid().ToString(),
                TransactionId = request.Transaction.TransactionId,
                CreditAccountId = creditAccount.AccountId,
                DebitAccountId = debitAccount.AccountId,
                CreatedAt = DateTime.Now,
                Currency = request.Transaction.Currency,
                Description = request.Transaction.Description,
                TransactionType = TransactionType.Transfer,
                TransactionDate = request.Transaction.TransactionDate.ToUniversalTime(),
                Type = DocumentType.Node,
                Document = NodeData.Transaction
            };

            var response = _Client.IndexDocument(transaction);
            return transaction;
        }
        catch (Exception Exception)
        {
            throw new ValidateErrorException("There were issues in completing the Transaction " + Exception.Message);
        }

    }
    private DeviceDocument AddDevice(DeviceRequest request, CustomerDocument customer, TransactionDocument transaction)
    {
        try
        {
            var ProfileRequest = _Client.Search<DeviceDocument>(c =>
            c.Query(q => q.Bool(
                b => b.Filter(f =>
                f.Bool(b => b.Should(sh => sh.MatchPhrase(mp => mp.Field(f => f.DeviceId).Query(request.DeviceId)))),
                f => f.Bool(b => b.Should(sh => sh.MatchPhrase(mp => mp.Field(f => f.CustomerId).Query(customer.CustomerId))))
                )
            )));
            if (ProfileRequest.Documents.Count <= 0)
            {
                var profile = new DeviceDocument
                {
                    CustomerId = customer.CustomerId,
                    ProfileId = Guid.NewGuid().ToString(),
                    DeviceId = request.DeviceId,
                    DeviceType = request.DeviceType,
                    IpAddress = request.IpAddress,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Type = DocumentType.Node,
                    Document = NodeData.Device
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
       /* if (request.ObservatoryId <= 0)
        {
            errors.Add("Please Specify what observatory you are monitoring");
        }*/

        return errors;
    }

    public async Task<bool> DownloadFileAndIngest(FileData data)
    {
        var awsAccessKey = _configuration.GetValue<string>("AWS:AccessKey");
        var awsSecretKey = _configuration.GetValue<string>("AWS:SecretKey");
        var awsBucketName = _configuration.GetValue<string>("AWS:BucketName");
        var awsRegion = _configuration.GetValue<string>("AWS:Region");

        if (string.IsNullOrEmpty(awsAccessKey) || string.IsNullOrEmpty(awsSecretKey) ||
            string.IsNullOrEmpty(awsBucketName) || string.IsNullOrEmpty(awsRegion))
        {
            throw new ValidateErrorException("AWS configuration is invalid.");
        }

        try
        {
            var s3Client = new AmazonS3Client(awsAccessKey, awsSecretKey, Amazon.RegionEndpoint.GetBySystemName(awsRegion));
            var request = new GetObjectRequest
            {
                BucketName = awsBucketName,
                Key = data.Name
            };
            using (var response = await s3Client.GetObjectAsync(request))
            using (var memoryStream = new MemoryStream())
            {
                await response.ResponseStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                using (var reader = new StreamReader(memoryStream))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true
                }))
                {

                    var records = csv.GetRecords<TransactionCsvRecord>().ToList();

                    var ingestQueueName = _configuration.GetValue<string>("IngestQueueName");
                    if (string.IsNullOrEmpty(ingestQueueName))
                    {
                        throw new ValidateErrorException("Invalid Ingest Queue Name");
                    }

                    foreach (var record in records)
                    {
                        var requestRecord = MakeRequest(record);
                        var serializedRecord = JsonConvert.SerializeObject(requestRecord);
                        await _queuePublisherService.PublishAsync(ingestQueueName, serializedRecord);
                    }
                    var document = _context.TransactionFileDocument.FirstOrDefault(d => d.Name == data.Name);
                    if (document == null)
                    {
                        throw new ValidateErrorException("Invalid Document");
                    }

                    document.Indexed = true;
                    _context.Update(document);
                    _context.SaveChanges();
                }
            }

            return true;
        }
        catch (AmazonS3Exception ex)
        {
            throw new ValidateErrorException($"Error encountered on server when downloading file from S3: {ex.Message}");
        }
        catch (Exception ex)
        {
            throw new ValidateErrorException($"Internal server error: {ex.Message}");
        }
    }

    public async Task<bool> CompleteIngestion()
    {
        var documents = _context.TransactionFileDocument.Where(d => d.Indexed == false).ToList();
        foreach (var document in documents)
        {
            var data = new FileData
            {
                Name = document.Name,
                Url = document.Url,
                ObservatoryId = document.ObservatoryId
            };
            await DownloadFileAndIngest(data);
        }
        return false;
    }
}