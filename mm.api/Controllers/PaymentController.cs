using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mm.api.Dtos;
using mm.api.Models;
using mm.api.Repository;
using System.Net;
using System.Web.Http.ModelBinding;

namespace mm.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly PaymentRepository _paymentRepository;
        public PaymentController(PaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }

        [Authorize(Roles = "Admin,User")]
        [HttpPost("MakePayment")]
        public async Task<ApiResponse<PaymentResponseDto>> MakePayment([FromBody] PaymentDto paymentDto)
        {
            if (!ModelState.IsValid)
            {
                return new ApiResponse<PaymentResponseDto>(HttpStatusCode.BadRequest, "Invalid Data", ModelState);
            }
            try
            {
                var response = await _paymentRepository.MakePaymentAsync(paymentDto);
                return new ApiResponse<PaymentResponseDto>(response, response.Message);
            }
            catch (Exception ex)
            {
                return new ApiResponse<PaymentResponseDto>(HttpStatusCode.InternalServerError, "Internal Server Error: " + ex.Message);
            }
        }

        [Authorize(Roles = "Admin,User")]
        [HttpGet("PaymentDetails/{id}")]
        public async Task<ApiResponse<Payment>> GetPaymentDetails(int id)
        {
            try
            {
                var payment = await _paymentRepository.GetPaymentDetailsAsync(id);
                if (payment == null)
                {
                    return new ApiResponse<Payment>(HttpStatusCode.NotFound, $"Payment with ID {id} not found.");
                }
                return new ApiResponse<Payment>(payment, "Payment retrieved successfully.");
            }
            catch (Exception ex)
            {
                return new ApiResponse<Payment>(HttpStatusCode.InternalServerError, "Internal server error: " + ex.Message);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("UpdatePaymentStatus/{id}")]
        public async Task<ApiResponse<UpdatePaymentResponseDto>> UpdatePaymentStatus(int id, [FromBody] PaymentStatusDto paymentStatusDTO)
        {
            if (!ModelState.IsValid)
            {
                return new ApiResponse<UpdatePaymentResponseDto>(HttpStatusCode.BadRequest, "Invalid data", ModelState);
            }
            if (id != paymentStatusDTO.PaymentId)
            {
                return new ApiResponse<UpdatePaymentResponseDto>(HttpStatusCode.BadRequest, "Mismatched Payment ID");
            }
            try
            {
                var response = await _paymentRepository.UpdatePaymentStatusAsync(id, paymentStatusDTO.Status);
                return new ApiResponse<UpdatePaymentResponseDto>(response, response.Message);
            }
            catch (Exception ex)
            {
                return new ApiResponse<UpdatePaymentResponseDto>(HttpStatusCode.InternalServerError, "Internal server error: " + ex.Message);
            }
        }
    }
}
