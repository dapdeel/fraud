using Api.DTOs;
using Api.CustomException;
using Api.Interfaces;
using Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Api.Services.Interfaces;

[Route("api/[controller]")]
[ApiController]
[AllowAnonymous]
public class PingController : ControllerBase
{
    private IGraphService _graphService;
    public PingController(IGraphService graphService)
    {
        _graphService = graphService;
    }
    [HttpGet("Start")]
    public IActionResult Start()
    {
        try
        {

            return Ok(new ApiResponse<dynamic>
            {
                Status = "success",
                Data = "Successful"
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

    [HttpGet("GraphTest")]
    public IActionResult Test()
    {
        try
        {
            var connector = _graphService.connect();
            var g = connector.traversal();
            return Ok(new ApiResponse<dynamic>
            {
                Status = "success",
                Data = "Successful"
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