using mm.api.Dtos;
using mm.api.Models;

namespace mm.api.Repository.Interface
{
    public interface IPaymentRepository
    {
        Task<PaymentResponseDto> MakePaymentAsync(PaymentDto paymentDto);
        Task<UpdatePaymentResponseDto> UpdatePaymentStatusAsync(int paymentId, string newStatus);
        Task<Payment?> GetPaymentDetailsAsync(int paymentId);
    }
}
