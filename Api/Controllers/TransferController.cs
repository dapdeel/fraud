using System.Security.Claims;
using Api.DTOs;
using Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TransferController : ControllerBase
{
    private IGraphService _graphService;
    public TransferController(IGraphService graphService)
    {
        _graphService = graphService;
    }
    [HttpPost("ingest")]
    public async Task<IActionResult> Ingest()
    {
        var response = _graphService.connect();
        return Ok(new ApiResponse<object>
        {
            Status = "success",
            Message = "User registered successfully",
            Data = new { }
        });
    }
    [HttpGet("getUser")]
    public async Task<IActionResult> GetUser(){

         var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId != null)
        {
            return Ok(new { UserId = userId });
        }

        return Unauthorized();
    }
}