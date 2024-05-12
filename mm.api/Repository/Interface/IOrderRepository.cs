using mm.api.Dtos;
using mm.api.Models;

namespace mm.api.Repository.Interface
{
    public interface IOrderRepository
    {
        Task<List<Order>> GetAllOrdersAsync(string status);
        Task<CreateOrderResponseDto> CreateOrderAsync(OrderDto orderDto);
        Task<ConfirmOrderResponseDto> ConfirmOrderAsync(int orderId);
        Task<OrderStatusResponseDto> UpdateOrderStatusAsync(int orderId, string newStatus);
        Task<Order?> GetOrderDetailsAsync(int orderId);
    }
}
