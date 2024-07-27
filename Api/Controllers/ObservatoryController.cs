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
    private IObservatoryService _service;
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
                Message = "Observatory Created Successfully",
                Data = new { response }
            });
        }
        catch (CustomServiceException Exception)
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
    public async Task<IActionResult> Invite([FromBody] ObservatoryRequest request)
    {
        var response = _service.Add(request, "");
        return Ok(new ApiResponse<object>
        {
            Status = "success",
            Message = "User registered successfully",
            Data = new { }
        });
    }
    [HttpPost("Accept/{id}")]
    public async Task<IActionResult> Accept(string id)
    {
        var response = _service.Errors();
        return Ok(new ApiResponse<object>
        {
            Status = "success",
            Message = "User registered successfully",
            Data = new { }
        });
    }
}
