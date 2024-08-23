using System.Security.Claims;
using Api.DTOs;
using Api.CustomException;
using Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Hangfire;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TransferController : ControllerBase
{
    private IObservatoryService _observatoryService;
    private IQueuePublisherService _queuePublisherService;
    private ITransferService _service;
    private ITransactionIngestGraphService _transactionIngestGraphService;
    private readonly IConfiguration _configuration;
    public TransferController(IObservatoryService observatoryService,
     IQueuePublisherService queuePublisherService, IConfiguration configuration,
     ITransactionIngestGraphService transactionIngestGraphService,
     ITransferService Service)
    {
        _observatoryService = observatoryService;
        _queuePublisherService = queuePublisherService;
        _transactionIngestGraphService = transactionIngestGraphService;
        _service = Service;
        _configuration = configuration;
    }
    [HttpPost("IngestAndQueue")]
    public async Task<IActionResult> IngestAndQueue(TransactionTransferRequest request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var observatory = await _observatoryService.Get(request.ObservatoryId, userId);
            var requestString = JsonConvert.SerializeObject(request);
            var queueName = _configuration.GetValue<string>("IngestQueueName");
            _queuePublisherService.Publish(queueName, requestString);

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
    [HttpPost("ingest")]
    public async Task<IActionResult> Ingest(TransactionTransferRequest request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var observatory = await _observatoryService.Get(request.ObservatoryId, userId);
            var response = await _service.Ingest(request, true);

            return Ok(new ApiResponse<object>
            {
                Status = "success",
                Message = "Successfully Added Record",
                Data = response
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

    [HttpPost("UploadAndIngest/{Id}")]
    public async Task<IActionResult> UploadIngest(int ObservatoryId, IFormFile file)
    {
        try
        {
            var response = await _service.UploadAndIngest(ObservatoryId, file);

            return Ok(new ApiResponse<object>
            {
                Status = "success",
                Message = "Successfully Added Record",
                Data = response
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
    [HttpPost("RunAnalysis/{ObservatoryId}")]
    public async Task<IActionResult> RunAnalysis(int ObservatoryId)
    {
        try
        {
            var response = BackgroundJob.Enqueue(() => _transactionIngestGraphService.RunAnalysis(ObservatoryId));

            return Ok(new ApiResponse<object>
            {
                Status = "success",
                Message = "Successfully Added Record",
                Data = response
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

    [HttpGet("getUser")]
    public IActionResult GetUser()
    {

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId != null)
        {
            return Ok(new { UserId = userId });
        }

        return Unauthorized();
    }
}