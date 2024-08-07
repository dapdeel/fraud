using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Api.DTOs;
using Api.CustomException;
using Api.Services.Interfaces;
using Newtonsoft.Json;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TransactionSummaryController : ControllerBase
{
    private IObservatoryService _observatoryService;
    private ITransactionSummaryService _summaryService;
    private readonly IConfiguration _configuration;
    public TransactionSummaryController(IObservatoryService observatoryService,
    ITransactionSummaryService summaryService, IConfiguration configuration)
    {
        _observatoryService = observatoryService;
        _summaryService = summaryService;
        _configuration = configuration;
    }
    [HttpGet("ObservatoryDashboard")]
    public async Task<IActionResult> ObservatoryDashboard(ObservatorySummaryRequest request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var observatory = await _observatoryService.Get(request.ObservatoryId, userId);
            var response = _summaryService.GetObservatorySummary(request);

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