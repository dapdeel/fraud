using Api.DTOs;
using Api.Services.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
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
}