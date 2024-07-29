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
    private ITransferService _transerService;
    public TransferController(IGraphService graphService, ITransferService transferService)
    {
        _graphService = graphService;
        _transerService = transferService;
    }
    [HttpPost("ingest")]
    public async Task<IActionResult> Ingest(TransactionTransferRequest request)
    {
        var response = await _transerService.Ingest(request);

        return Ok(new ApiResponse<object>
        {
            Status = "success",
            Message = "User registered successfully",
            Data = new { }
        });
    }
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