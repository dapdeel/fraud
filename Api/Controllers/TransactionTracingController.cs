using Api.CustomException;
using Api.DTOs;
using Api.Entity;
using Api.Interfaces;
using Api.Services.TransactionTracing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TransactionTracingController : ControllerBase
{
    public ITransactionTracingService _TracingService;
    public IAccountService _AccountService;
    public TransactionTracingController(ITransactionTracingService TracingService, IAccountService accountService)
    {
        _TracingService = TracingService;
        _AccountService = accountService;   
    }
    [HttpGet("Start/{ObservatoryId}/{TransactionId}")]
    public IActionResult Start(int ObservatoryId, string TransactionId)
    {
        try
        {
            var response =  _TracingService.GetTransactionById(ObservatoryId, TransactionId);
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

    [HttpGet("Trace/{Date}/{AccountNumber}/{BankId}/{CountryCode}")]
    public IActionResult GetFutureTransactions(DateTime Date, string AccountNumber, int BankId, string CountryCode)
    {
        try
        {
            var response = _TracingService.GetFutureTransactions(Date, AccountNumber, BankId, CountryCode);
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

    [HttpGet("AccountDetails/{AccountNumber}/{BankId}")]
    public IActionResult GetAccountDetails(string AccountNumber, int BankId)
    {
        try
        {
            var customerDetails = _AccountService.GetAccountDetails(AccountNumber, BankId);

            return Ok(new ApiResponse<object>
            {
                Status = "success",
                Message = "Customer details retrieved successfully",
                Data = customerDetails
            });
        }
        catch (ValidateErrorException ex)
        {
            return BadRequest(new ApiResponse<object>
            {
                Status = "ValidationError",
                Message = ex.Message,
                Error = new ApiError { Code = "400", Details = ex.Message }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<object>
            {
                Status = "error",
                Message = "An error occurred while retrieving account and customer details",
                Error = new ApiError { Code = "500", Details = ex.Message }
            });
        }
    }


    [HttpGet("accounts/count")]
    public IActionResult GetAccountsCount()
    {
        try
        {
            var count = _AccountService.GetAccountCount();
            return Ok(new ApiResponse<long>
            {
                Status = "success",
                Data = count
            });
        }
        catch (ValidateErrorException ex)
        {
            return BadRequest(new ApiResponse<dynamic>
            {
                Status = "ValidationError",
                Error = new ApiError { Code = "", Details = ex.Message },
                Message = ex.Message
            });
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, new ApiResponse<dynamic>
            {
                Status = "error",
                Message = "An unexpected error occurred.",
                Error = new ApiError { Code = "", Details = ex.Message }
            });
        }
    }

    [HttpPost("accounts/all")]
    public IActionResult GetAllAccounts([FromBody] AccountListRequest accountRequest)
    {
        try
        {
            var accounts = _AccountService.GetAccountsByPage(accountRequest.PageNumber, accountRequest.BatchSize);
            return Ok(new ApiResponse<List<AccountWithDetailsDto>>
            {
                Status = "success",
                Data = accounts
            });
        }
        catch (ValidateErrorException ex)
        {
            return BadRequest(new ApiResponse<dynamic>
            {
                Status = "ValidationError",
                Error = new ApiError { Code = "", Details = ex.Message },
                Message = ex.Message
            });
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, new ApiResponse<dynamic>
            {
                Status = "error",
                Message = "An unexpected error occurred.",
                Error = new ApiError { Code = "", Details = ex.Message }
            });
        }
    }


}