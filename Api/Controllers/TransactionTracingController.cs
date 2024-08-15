using Api.CustomException;
using Api.DTOs;
using Api.Services.TransactionTracing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TransactionTracingController : ControllerBase
{
    public ITransactionTracingService _TracingService;
    public TransactionTracingController(ITransactionTracingService TracingService)
    {
        _TracingService = TracingService;
    }
    [HttpGet("Start/{ObservatoryId}/{TransactionId}")]
    public IActionResult Start(int ObservatoryId, string TransactionId)
    {
        try
        {
            var response = _TracingService.GetTransactionById(ObservatoryId, TransactionId);
            return Ok(new ApiResponse<object>
            {
                Status = "success",
                Message = "Success",
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
    [HttpGet("Trace/{Date}/{AccountNumber}/{BankCode}/{CountryCode}")]
    public IActionResult GetFutureTransactions(DateTime Date, string AccountNumber, string BankCode, string CountryCode)
    {
        try
        {
            var response = _TracingService.GetFutureTransactions(Date, AccountNumber, BankCode, CountryCode);
            return Ok(new ApiResponse<object>
            {
                Status = "success",
                Message = "Success",
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
    [HttpGet("Account/{ObservatoryId}/{NodeId}")]
    public IActionResult GetAccountDetails(int ObservatoryId, int NodeId)
    {
        try
        {
            var response = _TracingService.GetAccountNode(ObservatoryId, NodeId);
            return Ok(new ApiResponse<object>
            {
                Status = "success",
                Message = "Success",
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
}