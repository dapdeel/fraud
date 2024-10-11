using Api.CustomException;
using Api.Data;
using Api.DTOs;
using Api.Interfaces;
using Api.Migrations;
using Api.Models;
using Api.Services.Interfaces;
using Gremlin.Net.Process.Traversal;

public class AccountService : IAccountService
{
    private readonly ApplicationDbContext _context;
    private JanusGraphConnector? _connector;
    private readonly IElasticSearchService _elasticSearchService;
    public AccountService(ApplicationDbContext context, IElasticSearchService elasticSearchService)
    {
        _context = context;
        _elasticSearchService = elasticSearchService;
    }
    public AccountDocument? GetByAccountNumberAndBankId(string AccountNumber, int bankId, Observatory Observatory)
    {
        var elasticClient = Observatory.UseDefault ?
                  _elasticSearchService.connect() :
                  _elasticSearchService.connect(Observatory.ElasticSearchHost);

        var searchResponse = elasticClient.Search<AccountDocument>(s => s
     .Query(q => q
         .Bool(b => b
             .Filter(f => f
                 .Bool(bb => bb
                     .Filter(ff => ff
                         .Bool(bbb => bbb
                             .Should(sh => sh
                                 .Term(t => t
                                     .Field("document.keyword")
                                     .Value("Account")
                                 )
                             )
                             .MinimumShouldMatch(1)
                         ),
                         fff => fff
                         .Bool(bbbb => bbbb
                             .Should(sh => sh
                                 .Match(m => m
                                     .Field("indexed")
                                     .Query("true")
                                 )
                             )
                             .MinimumShouldMatch(1)
                         ),
                         ffff => ffff
                         .Bool(bbbbb => bbbbb
                             .Should(sh => sh
                                 .Term(t => t
                                     .Field("accountNumber.keyword")
                                     .Value(AccountNumber)
                                 )
                             )
                             .MinimumShouldMatch(1)
                         ),
                         ffff => ffff
                         .Bool(bbbbb => bbbbb
                             .Should(sh => sh
                                 .Match(t => t
                                     .Field("bankId")
                                     .Query(bankId.ToString())
                                 )
                             )
                             .MinimumShouldMatch(1)
                         )
                     )
                 )
             )
         )
     )
 );

        return searchResponse.Documents.FirstOrDefault();
    }


    public AccountWithDetailsDto GetAccountDetails(string AccountNumber, int BankId)
    {
        var validObservatories = _context.Observatories
            .Where(o => o.ObservatoryType == ObservatoryType.Swtich ||
                        (o.ObservatoryType == ObservatoryType.Bank && o.BankId == BankId))
            .ToList();

        if (!validObservatories.Any())
        {
            throw new ValidateErrorException("No valid observatories found for the given bank");
        }

        var observatory = validObservatories.First();
        var accountDocument = GetByAccountNumberAndBankId(AccountNumber, BankId, observatory);
        if (accountDocument == null)
        {
            throw new ValidateErrorException("Account not found");
        }

        var customerId = accountDocument.CustomerId;
        if (string.IsNullOrEmpty(customerId))
        {
            throw new ValidateErrorException("Customer ID not found in account");
        }

        var elasticClient = observatory.UseDefault
            ? _elasticSearchService.connect()
            : _elasticSearchService.connect(observatory.ElasticSearchHost);

        var customerSearchResponse = elasticClient.Search<TransactionCustomerDto>(s => s
            .Index("transactions")
            .Query(q => q
                .Bool(b => b
                    .Must(m => m
                        .Term(t => t
                            .Field("document.keyword")
                            .Value("Customer")
                        ),
                        m => m
                        .Term(t => t
                            .Field("customerId.keyword")
                            .Value(customerId)
                        ),
                        m => m
                        .Term(t => t
                            .Field("indexed")
                            .Value(true)
                        ),
                        m => m
                        .Term(t => t
                            .Field("type.keyword")
                            .Value("Node")
                        )
                    )
                )
            )
        );

        var customerDocument = customerSearchResponse.Documents.FirstOrDefault();
        if (customerDocument == null)
        {
            throw new ValidateErrorException("Customer details not found");
        }
        return new AccountWithDetailsDto
        {
            AccountId = accountDocument.AccountId,
            AccountNumber = accountDocument.AccountNumber,
            AccountBalance = accountDocument.AccountBalance,
            FullName = customerDocument.FullName ?? "Unknown",
            Email = customerDocument.Email ?? "Unknown",
            Phone = customerDocument.Phone ?? "Unknown",
            CreatedAt = accountDocument.CreatedAt,
            UpdatedAt = accountDocument.UpdatedAt,
            BankId = BankId
        };
    }

    public long GetAccountCount()
    {
        var elasticClient = _elasticSearchService.connect();

        var searchResponse = elasticClient.Count<TransactionAccountDto>(s => s
            .Query(q => q
                .Bool(b => b
                    .Filter(f => f
                        .Term(t => t
                            .Field("document.keyword")
                            .Value("Account")
                        )
                        && f
                        .Term(t => t
                            .Field("indexed")
                            .Value(true)
                        )
                    )
                )
            )
        );

        if (!searchResponse.IsValid)
        {
            throw new ValidateErrorException("Unable to query Elasticsearch for account count.");
        }

        return searchResponse.Count;
    }

    public List<AccountWithDetailsDto> GetAccountsByPage(int pageNumber, int batch, string observatoryTag)
    {
        var elasticClient = _elasticSearchService.connect();
        int from = pageNumber * batch;
        int size = batch;

        var searchResponse = elasticClient.Search<TransactionAccountDto>(s => s
            .Index("transactions")
            .Query(q => q
                .Bool(b => b
                    .Filter(f => f
                        
                        .Term(t => t
                            .Field("document.keyword")
                            .Value("Account")
                        )
                
                        && f.Term(t => t
                            .Field("indexed")
                            .Value(true)
                        )
                   
                        && f.Term(t => t
                            .Field("observatoryTag.keyword") 
                            .Value(observatoryTag)
                        )
                    )
                )
            )
            .From(from)
            .Size(size)
        );

        if (!searchResponse.IsValid)
        {
            throw new ValidateErrorException("Unable to query Elasticsearch for accounts.");
        }

        var accountsWithDetails = new List<AccountWithDetailsDto>();

        foreach (var hit in searchResponse.Hits)
        {
            var account = hit.Source;

            var accountDetails = GetAccountDetails(account.AccountNumber, account.BankId);
            accountsWithDetails.Add(accountDetails);
        }

        return accountsWithDetails;
    }


    public List<AccountRelationshipResult> GetAccountRelationshipScore(string creditAccountId, string debitAccountId)
    {
        var elasticClient = _elasticSearchService.connect();

        var creditToDebitResponse = elasticClient.Search<TransferredEdgeDocumentDTO>(s => s
            .Query(q => q
                .Bool(b => b
                    .Filter(f => f
                        .Term(t => t.Field("type.keyword").Value("Edge"))
                        && f.Term(t => t.Field("document.keyword").Value("Transfered"))
                        && f.Term(t => t.Field("from.keyword").Value(creditAccountId))
                        && f.Term(t => t.Field("to.keyword").Value(debitAccountId))
                    )
                )
            )
        );

        var debitToCreditResponse = elasticClient.Search<TransferredEdgeDocumentDTO>(s => s
            .Query(q => q
                .Bool(b => b
                    .Filter(f => f
                        .Term(t => t.Field("type.keyword").Value("Edge"))
                        && f.Term(t => t.Field("document.keyword").Value("Transfered"))
                        && f.Term(t => t.Field("from.keyword").Value(debitAccountId))
                        && f.Term(t => t.Field("to.keyword").Value(creditAccountId))
                  )
                )
            )
        );

        var results = new List<AccountRelationshipResult>();

        var creditToDebitResult = new AccountRelationshipResult
        {
            RelationshipType = "credit-to-debit" 
        };
        if (creditToDebitResponse.Documents.Count > 0)
        {
            var transferedDocument = creditToDebitResponse.Documents.First();
            creditToDebitResult.TransferredDocument = transferedDocument;
            creditToDebitResult.RelationshipScore = transferedDocument.TransactionCount > 0
                ? transferedDocument.EMEA / transferedDocument.TransactionCount
                : 0;
        }
        else
        {
            creditToDebitResult.TransferredDocument = null;
            creditToDebitResult.RelationshipScore = 0;
        }
        results.Add(creditToDebitResult);

        var debitToCreditResult = new AccountRelationshipResult
        {
            RelationshipType = "debit-to-credit"  
        };
        if (debitToCreditResponse.Documents.Count > 0)
        {
            var transferedDocument = debitToCreditResponse.Documents.First();
            debitToCreditResult.TransferredDocument = transferedDocument;
            debitToCreditResult.RelationshipScore = transferedDocument.TransactionCount > 0
                ? transferedDocument.EMEA / transferedDocument.TransactionCount
                : 0;
        }
        else
        {
            debitToCreditResult.TransferredDocument = null;
            debitToCreditResult.RelationshipScore = 0;
        }
        results.Add(debitToCreditResult);

        return results;
    }



}