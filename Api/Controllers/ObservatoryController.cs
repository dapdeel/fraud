using System.Security.Claims;
using Api.DTOs;
using Api.Exception;
using Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

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
        catch (CustomServiceException ex)
        {
            return BadRequest(new ApiResponse<dynamic>
            {
                Status = "ValidationError",
                Error = new ApiError { Code = "", Details = ex.Message },
                Message = ex.Message
            });
        }
    }

    [HttpPost("Invite")]
    public async Task<IActionResult> Invite([FromBody] InvitationRequest request)
    {
        try
        {
            await _service.Invite(request);
            return Ok(new ApiResponse<object>
            {
                Status = "success",
                Message = "Invitation sent successfully",
                Data = new { }
            });
        }
        catch (CustomServiceException ex)
        {
            return BadRequest(new ApiResponse<dynamic>
            {
                Status = "ValidationError",
                Error = new ApiError { Code = "", Details = ex.Message },
                Message = ex.Message
            });
        }
    }

    [HttpPost("Accept/{id}")]
    public async Task<IActionResult> Accept(int id)
    {
        try
        {
            await _service.AcceptInvite(id);
            return Ok(new ApiResponse<object>
            {
                Status = "success",
                Message = "Invitation accepted successfully",
                Data = new { }
            });
        }
        catch (CustomServiceException ex)
        {
            return BadRequest(new ApiResponse<dynamic>
            {
                Status = "ValidationError",
                Error = new ApiError { Code = "", Details = ex.Message },
                Message = ex.Message
            });
        }
    }
}
