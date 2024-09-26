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
            var observatory = await _observatoryService.Get(request.ObservatoryTag, userId);
            var requestString = JsonConvert.SerializeObject(request);
            var queueName = _configuration.GetValue<string>("IngestQueueName");
            await _queuePublisherService.PublishAsync(queueName, requestString);

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

    [HttpPost("UploadAndIngest")]
    public async Task<IActionResult> UploadIngest([FromForm] UploadIngestRequest request, [FromForm] IFormFile file)
    {
        try
        {
            if(request.ObservatoryId <= 0){
                throw new ValidateErrorException("Invalid ObservatoryId");
            }
            var response = await _service.UploadAndIngest(request.ObservatoryTag, file);

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

    [HttpPost("DownloadAndIngest")]
    public async Task<IActionResult> DownloadAndIngest([FromBody] FileData fileData)
    {
        try
        {
            var response = await _service.DownloadFileAndIngest(fileData);

            return Ok(new ApiResponse<object>
            {
                Status = "success",
                Message = "Successfully Downloaded and Ingested File",
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
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<dynamic>
            {
                Status = "error",
                Error = new ApiError { Code = "", Details = ex.Message },
                Message = "An unexpected error occurred"
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