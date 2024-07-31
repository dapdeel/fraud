using Api.DTOs;
using Api.Exception;
using Api.Interfaces;
using Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BankController : ControllerBase
{
    public IBankService _bankService;
    public BankController(IBankService bankService)
    {
        _bankService = bankService;
    }
    [HttpGet("All")]
    public IActionResult All()
    {
        try
        {
            var banks = _bankService.All();

            return Ok(new ApiResponse<dynamic>
            {
                Status = "success",
                Data = banks
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
    [HttpPost("Add")]
    public IActionResult Add([FromBody] List<BankRequest> requests)
    {
        try
        {
            List<Bank> banks = [];
            foreach (var request in requests)
            {
                var bank = _bankService.Add(request);
                banks.Add(bank);
            }
            return Ok(new ApiResponse<dynamic>
            {
                Status = "success",
                Data = banks
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