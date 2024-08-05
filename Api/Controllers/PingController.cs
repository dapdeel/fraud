using Api.DTOs;
using Api.Exception;
using Api.Interfaces;
using Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
[AllowAnonymous]
public class PingController : ControllerBase
{

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
}