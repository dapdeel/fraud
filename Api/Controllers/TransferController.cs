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
    private IObservatoryService _observatoryService;
    private IQueuePublisherService _queuePublisherService;
    private ITransferService _service;
    private readonly IConfiguration _configuration;
    public TransferController(IObservatoryService observatoryService,
     IQueuePublisherService queuePublisherService, IConfiguration configuration, ITransferService Service)
    {
        _observatoryService = observatoryService;
        _queuePublisherService = queuePublisherService;
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
            var response =  await _service.Ingest(request);

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