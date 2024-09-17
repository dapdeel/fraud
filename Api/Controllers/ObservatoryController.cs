using System.Security.Claims;
using Api.DTOs;
using Api.CustomException;
using Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Api.Models.Responses;
using Api.Models;
using Api.Entity;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ObservatoryController : ControllerBase
{
    private readonly IObservatoryService _service;

    public ObservatoryController(IObservatoryService service)
    {
        _service = service;
    }

    [HttpPost("Add")]
    public async Task<IActionResult> Add([FromBody] ObservatoryRequest request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var response = await _service.Add(request, userId);
            return Ok(new ApiResponse<dynamic>
            {
                Status = "success",
                Message = "Observatory created successfully",
                Data = new { response }
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
    [HttpGet("Get/{Id}")]
    public async Task<IActionResult> Get(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var response = await _service.Get(id, userId);
            return Ok(new ApiResponse<dynamic>
            {
                Status = "success",
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

    [HttpPost("Invite")]
    public async Task<IActionResult> Invite([FromBody] InvitationRequest request, string adminUserId)
    {
        try
        {
            await _service.Invite(request, adminUserId);
            return Ok(new ApiResponse<object>
            {
                Status = "success",
                Message = "Invitation sent successfully",
                Data = new { }
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


    [HttpPost("AcceptInvite/{id}")]
    public async Task<IActionResult> AcceptInvite(int id, string userId)
    {
        try
        {
            await _service.AcceptInvite(id, userId);
            return Ok(new ApiResponse<object>
            {
                Status = "success",
                Message = "Invitation accepted successfully",
                Data = new { }
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


    [HttpPost("RejectInvite/{id}")]
    public async Task<IActionResult> RejectInvite(int id, string userId)
    {
        try
        {
            await _service.RejectInvite(id, userId);
            return Ok(new ApiResponse<object>
            {
                Status = "success",
                Message = "Invitation rejected successfully",
                Data = new { }
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

    [HttpGet("CheckUserStatus")]
    public async Task<IActionResult> CheckUserStatus([FromQuery] string userId)
    {
        try
        {
            var statusDto = await _service.CheckUserObservatoryStatus(userId);

            return Ok(new ApiResponse<UserObservatoryStatus>
            {
                Status = "success",
                Data = statusDto
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
                Message = "An unexpected error occurred.",
                Error = new ApiError { Code = "", Details = ex.Message }
            });
        }
    }
    [HttpGet("GetByUserId")]
    public async Task<IActionResult> GetObservatoriesByUserId(string userId)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse<dynamic>
                {
                    Status = "error",
                    Message = "User not authenticated"
                });
            }

            var observatories = await _service.GetObservatoriesByUserId(userId);
            return Ok(new ApiResponse<dynamic>
            {
                Status = "success",
                Data = observatories
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
                Message = "An unexpected error occurred.",
                Error = new ApiError { Code = "", Details = ex.Message }
            });
        }
    }


    [HttpGet("{userId}/invitations")]
    public async Task<ActionResult<List<Observatory>>> GetInvitedObservatories(string userId)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse<dynamic>
                {
                    Status = "error",
                    Message = "User not authenticated"
                });
            }

            var observatories = await _service.GetInvitedObservatoriesByUserId(userId);
            return Ok(new ApiResponse<dynamic>
            {
                Status = "success",
                Data = observatories
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
                Message = "An unexpected error occurred.",
                Error = new ApiError { Code = "", Details = ex.Message }
            });
        }
    }
    [HttpPost("Switch")]
    public async Task<IActionResult> Switch([FromBody] SwitchRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.UserId) || request.ObservatoryId <= 0)
            {
                return BadRequest(new ApiResponse<dynamic>
                {
                    Status = "ValidationError",
                    Message = "UserId and ObservatoryId are required"
                });
            }

            var observatory = await _service.SwitchCurrentObservatory(request.UserId, request.ObservatoryId);

            return Ok(new ApiResponse<Observatory>
            {
                Status = "success",
                Message = "Observatory switched successfully",
                Data = observatory 
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
                Message = "An unexpected error occurred",
                Error = new ApiError { Code = "", Details = ex.Message }
            });
        }
    }



    [HttpGet("Current")]
    public async Task<IActionResult> GetCurrent([FromQuery] string userId)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new ApiResponse<dynamic>
                {
                    Status = "ValidationError",
                    Message = "UserId is required"
                });
            }
            var currentObservatory = await _service.GetCurrentObservatory(userId);

            if (currentObservatory == null)
            {
                return NotFound(new ApiResponse<dynamic>
                {
                    Status = "error",
                    Message = "Current observatory not found"
                });
            }

            return Ok(new ApiResponse<dynamic>
            {
                Status = "success",
                Data = currentObservatory
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
                Message = "An unexpected error occurred",
                Error = new ApiError { Code = "", Details = ex.Message }
            });
        }
    }
    [HttpGet("{observatoryId}/transaction-rules")]
    public async Task<IActionResult> GetTransactionRules(int observatoryId)
    {
        try
        {
            var rules = await _service.GetTransactionRules(observatoryId);
            if (rules == null)
            {
                return NotFound("Transaction rules not found for this observatory.");
            }

            return Ok(rules);
        }
        catch (ValidateErrorException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            // Log the exception as needed
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // Update Transaction Rules for a given Observatory
    [HttpPut("{observatoryId}/transaction-rules")]
    public async Task<IActionResult> UpdateTransactionRules(int observatoryId, [FromBody] TransactionRules rulesDto)
    {
        if (rulesDto == null || observatoryId != rulesDto.ObservatoryId)
        {
            return BadRequest("Invalid request.");
        }

        try
        {
            await _service.UpdateTransactionRules(observatoryId, rulesDto);
            return NoContent(); // Successful update
        }
        catch (ValidateErrorException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            // Log the exception as needed
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}
