using Api.DTOs;
using Api.CustomException; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Api.Services.Interfaces;
using Api.Entity;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class FlaggingController : ControllerBase
{
    private readonly IFlaggingService _flaggingService;

    public FlaggingController(IFlaggingService flaggingService)
    {
        _flaggingService = flaggingService;
    }

    [HttpPost("MarkTransactionAsSuspicious")]
    public IActionResult MarkTransactionAsSuspicious([FromBody] FlagTransactionRequest request)
    {
        try
        {
            var result = _flaggingService.MarkTransactionAsSuspicious(request.ObservatoryTag, request.TransactionId).Result;

            return Ok(new ApiResponse<dynamic>
            {
                Status = "success",
                Data = result
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
    }

    [HttpPost("MarkTransactionAsBlacklisted")]
    public IActionResult MarkTransactionAsBlacklisted([FromBody] FlagTransactionRequest request)
    {
        try
        {
            var result = _flaggingService.MarkTransactionAsBlacklisted(request.ObservatoryTag, request.TransactionId).Result;

            return Ok(new ApiResponse<dynamic>
            {
                Status = "success",
                Data = result
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
    }

    [HttpPost("MarkAccountAsSuspicious")]
    public IActionResult MarkAccountAsSuspicious([FromBody] FlagAccountRequest request)
    {
        try
        {
            var result = _flaggingService.MarkAccountAsSuspicious(request.AccountNumber, request.BankId).Result;

            return Ok(new ApiResponse<dynamic>
            {
                Status = "success",
                Data = result
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
    }

    [HttpPost("MarkAccountAsBlacklisted")]
    public IActionResult MarkAccountAsBlacklisted([FromBody] FlagAccountRequest request)
    {
        try
        {
            var result = _flaggingService.MarkAccountAsBlacklistedAsync(request.AccountNumber, request.BankId).Result;

            return Ok(new ApiResponse<dynamic>
            {
                Status = "success",
                Data = result
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
    }

    [HttpGet("GetBlacklistedTransactions")]
    public IActionResult GetBlacklistedTransactions(string observatoryTag)
    {
        try
        {
            var result = _flaggingService.GetBlacklistedTransactions(observatoryTag);

            if (!result.Any())
            {
                return Ok(new ApiResponse<List<BlacklistedTransaction>>
                {
                    Status = "success",
                    Message = "No blacklisted transactions found for the provided observatory."
                });
            }

            return Ok(new ApiResponse<List<BlacklistedTransaction>>
            {
                Status = "success",
                Data = result
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
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<dynamic>
            {
                Status = "error",
                Message = $"An unexpected error occurred: {ex.Message}"
            });
        }
    }

    [HttpGet("GetSuspiciousTransactions")]
    public IActionResult GetSuspiciousTransactions(string observatoryTag)
    {
        try
        {
            var result = _flaggingService.GetSuspiciousTransactions(observatoryTag);

            if (!result.Any())
            {
                return Ok(new ApiResponse<List<SuspiciousTransaction>>
                {
                    Status = "success",
                    Message = "No suspicious transactions found for the provided observatory."
                });
            }

            return Ok(new ApiResponse<List<SuspiciousTransaction>>
            {
                Status = "success",
                Data = result
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
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<dynamic>
            {
                Status = "error",
                Message = $"An unexpected error occurred: {ex.Message}"
            });
        }
    }
    [HttpGet("GetBlacklistedAccounts")]
    public IActionResult GetBlacklistedAccounts(string observatoryTag)
    {
        try
        {
            var result = _flaggingService.GetBlacklistedAccounts(observatoryTag);

            if (!result.Any())
            {
                return Ok(new ApiResponse<List<BlacklistedAccount>>
                {
                    Status = "success",
                    Message = "No blacklisted accounts found for the provided observatory."
                });
            }

            return Ok(new ApiResponse<List<BlacklistedAccount>>
            {
                Status = "success",
                Data = result
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
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<dynamic>
            {
                Status = "error",
                Message = $"An unexpected error occurred: {ex.Message}"
            });
        }
    }
    [HttpGet("GetSuspiciousAccounts")]
    public IActionResult GetSuspiciousAccounts(string observatoryTag)
    {
        try
        {
            var result = _flaggingService.GetSuspiciousAccounts(observatoryTag);

            if (!result.Any())
            {
                return Ok(new ApiResponse<List<SuspiciousAccount>>
                {
                    Status = "success",
                    Message = "No suspicious accounts found for the provided observatory."
                });
            }

            return Ok(new ApiResponse<List<SuspiciousAccount>>
            {
                Status = "success",
                Data = result
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
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<dynamic>
            {
                Status = "error",
                Message = $"An unexpected error occurred: {ex.Message}"
            });
        }
    }
}
