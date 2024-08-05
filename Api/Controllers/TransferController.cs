using System.Security.Claims;
using Api.DTOs;
using Api.CustomException;
using Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TransferController : ControllerBase
{
    private IGraphService _graphService;
    private ITransferService _transerService;
    private IQueuePublisherService _queuePublisherService;
    private readonly IConfiguration _configuration;
    public TransferController(IGraphService graphService, ITransferService transferService,
     IQueuePublisherService queuePublisherService, IConfiguration configuration)
    {
        _graphService = graphService;
        _transerService = transferService;
        _queuePublisherService = queuePublisherService;
        _configuration = configuration;
    }
    [HttpPost("ingest")]
    public IActionResult Ingest(TransactionTransferRequest request)
    {
        try
        {
            var requestString = JsonConvert.SerializeObject(request);
            var queueName = _configuration.GetValue<string>("IngestQueueName");
            _queuePublisherService.Publish(queueName, requestString);
            // var response = await _transerService.Ingest(request);

            return Ok(new ApiResponse<object>
            {
                Status = "success",
                Message = "Record Queued successfully",
                Data = new { }
            });
        }
        catch (ValidateErrorException Exception)
        {
            return BadRequest(new ApiResponse<dynamic>
            {
                Status = "ValidationError",
                Error = new ApiError { Code = "", Details = Exception.Message },
                Message = Exception.Message
            });
        }
    }
    //
    [HttpGet("getUser")]
    public async Task<IActionResult> GetUser()
    {

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId != null)
        {
            return Ok(new { UserId = userId });
        }

        return Unauthorized();
    }
}