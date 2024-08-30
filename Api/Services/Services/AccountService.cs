using Api.Data;
using Api.Interfaces;
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
                                     .Field("type.keyword")
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
}