using Api.Models; 
using Api.Services.TransactionTracing;
using Api.Exception;
using Api.Models.Responses;
using Microsoft.AspNetCore.Mvc;
using System;
using Api.DTOs;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionTracingService _transactionService;

        public TransactionController(ITransactionTracingService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpGet("total-transactions/last-30-days/{accountNumber}")]
        public async Task<IActionResult> GetTotalTransactionsLast30Days(string accountNumber)
        {
            try
            {
                var totalTransactions = await _transactionService.GetTotalTransactionsLast30Days(accountNumber);
                return Ok(new ApiResponse<int>
                {
                    Status = "success",
                    Data = totalTransactions
                });
            }
            catch (ValidateErrorException ex)
            {
                return BadRequest(new ApiResponse<dynamic>
                {
                    Status = "ValidationError",
                    Error = new ApiError { Code = "", Details = ex.Message },
                    Message = ex.Message
                });
            }
            catch (System.Exception ex) 
            {
                return StatusCode(500, new ApiResponse<dynamic>
                {
                    Status = "error",
                    Message = "An unexpected error occurred.",
                    Error = new ApiError { Code = "", Details = ex.Message }
                });
            }
        }

        [HttpGet("total-amount/last-30-days/{accountNumber}")]
        public async Task<IActionResult> GetTotalAmountLast30Days(string accountNumber)
        {
            try
            {
                var totalAmount = await _transactionService.GetTotalAmountLast30Days(accountNumber);
                return Ok(new ApiResponse<decimal>
                {
                    Status = "success",
                    Data = totalAmount
                });
            }
            catch (ValidateErrorException ex)
            {
                return BadRequest(new ApiResponse<dynamic>
                {
                    Status = "ValidationError",
                    Error = new ApiError { Code = "", Details = ex.Message },
                    Message = ex.Message
                });
            }
            catch (System.Exception ex) 
            {
                return StatusCode(500, new ApiResponse<dynamic>
                {
                    Status = "error",
                    Message = "An unexpected error occurred.",
                    Error = new ApiError { Code = "", Details = ex.Message }
                });
            }
        }

        [HttpGet("transactions/last-hour/{accountNumber}")]
        public async Task<IActionResult> GetTransactionsLastHour(string accountNumber)
        {
            try
            {
                var transactionsLastHour = await _transactionService.GetTransactionsLastHour(accountNumber);
                return Ok(new ApiResponse<int>
                {
                    Status = "success",
                    Data = transactionsLastHour
                });
            }
            catch (ValidateErrorException ex)
            {
                return BadRequest(new ApiResponse<dynamic>
                {
                    Status = "ValidationError",
                    Error = new ApiError { Code = "", Details = ex.Message },
                    Message = ex.Message
                });
            }
            catch (System.Exception ex) 
            {
                return StatusCode(500, new ApiResponse<dynamic>
                {
                    Status = "error",
                    Message = "An unexpected error occurred.",
                    Error = new ApiError { Code = "", Details = ex.Message }
                });
            }
        }

        [HttpGet("all/{accountNumber}")]
        public async Task<IActionResult> GetAllTransactions(string accountNumber)
        {
            try
            {
                var transactions = await _transactionService.GetAllTransactions(accountNumber);
                return Ok(new ApiResponse<List<Transaction>>
                {
                    Status = "success",
                    Data = transactions
                });
            }
            catch (ValidateErrorException ex)
            {
                return BadRequest(new ApiResponse<dynamic>
                {
                    Status = "ValidationError",
                    Error = new ApiError { Code = "", Details = ex.Message },
                    Message = ex.Message
                });
            }
            catch (System.Exception ex) 
            {
                return StatusCode(500, new ApiResponse<dynamic>
                {
                    Status = "error",
                    Message = "An unexpected error occurred.",
                    Error = new ApiError { Code = "", Details = ex.Message }
                });
            }
        }

        [HttpGet("sent-accounts")]
        public async Task<IActionResult> GetSentAccounts(string accountNumber, DateTime startDate, DateTime endDate)
        {
            try
            {
                var sentAccounts = await _transactionService.GetSentAccounts(accountNumber, startDate, endDate);
                return Ok(new ApiResponse<dynamic>
                {
                    Status = "success",
                    Data = sentAccounts
                });
            }
            catch (ValidateErrorException ex)
            {
                return BadRequest(new ApiResponse<dynamic>
                {
                    Status = "ValidationError",
                    Error = new ApiError { Code = "", Details = ex.Message },
                    Message = ex.Message
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new ApiResponse<dynamic>
                {
                    Status = "error",
                    Message = "An unexpected error occurred.",
                    Error = new ApiError { Code = "", Details = ex.Message }
                });
            }
        }


        [HttpGet("{transactionId}")]
        public async Task<IActionResult> GetTransactionById(int transactionId)
        {
            try
            {
                var transaction = await _transactionService.GetTransactionById(transactionId);
                if (transaction == null)
                {
                    return NotFound(new ApiResponse<dynamic>
                    {
                        Status = "NotFound",
                        Message = "Transaction not found",
                        Data = new { }
                    });
                }
                return Ok(new ApiResponse<Transaction>
                {
                    Status = "success",
                    Data = transaction
                });
            }
            catch (ValidateErrorException ex)
            {
                return BadRequest(new ApiResponse<dynamic>
                {
                    Status = "ValidationError",
                    Error = new ApiError { Code = "", Details = ex.Message },
                    Message = ex.Message
                });
            }
            catch (System.Exception ex) 
            {
                return StatusCode(500, new ApiResponse<dynamic>
                {
                    Status = "error",
                    Message = "An unexpected error occurred.",
                    Error = new ApiError { Code = "", Details = ex.Message }
                });
            }
        }

        [HttpGet("customer/{customerId}")]
        public async Task<IActionResult> GetTransactionsByCustomerId(string customerId) 
        {
            try
            {
                var transactions = await _transactionService.GetTransactionsByCustomerId(customerId);
                return Ok(new ApiResponse<List<Transaction>>
                {
                    Status = "success",
                    Data = transactions
                });
            }
            catch (ValidateErrorException ex)
            {
                return BadRequest(new ApiResponse<dynamic>
                {
                    Status = "ValidationError",
                    Error = new ApiError { Code = "", Details = ex.Message },
                    Message = ex.Message
                });
            }
            catch (System.Exception ex) 
            {
                return StatusCode(500, new ApiResponse<dynamic>
                {
                    Status = "error",
                    Message = "An unexpected error occurred...",
                    Error = new ApiError { Code = "", Details = ex.Message }
                });
            }
        }
    }
}
