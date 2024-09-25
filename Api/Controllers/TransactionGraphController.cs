using Api.CustomException;
using Api.DTOs;
using Api.Services.TransactionTracing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TransactionGraphController : ControllerBase
{
    public ITransactionTracingGraphService _TracingGraphService;
    public TransactionGraphController(ITransactionTracingGraphService TracingGraphService)
    {
        _TracingGraphService = TracingGraphService;
    }
    [HttpGet("Node/{NodeId}")]
    public IActionResult Node(long NodeId)
    {
        try
        {
            var response = _TracingGraphService.NodeDetails(NodeId);
            return Ok(new ApiResponse<object>
            {
                Status = "success",
                Message = "Success",
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

}