using Microsoft.AspNetCore.Mvc;
using mm.api.Dtos;
using mm.api.Models;
using mm.api.Repository;
using System.Net;

namespace mm.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly OrderRepository _orderRepository;
        public OrderController(OrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }
        // GET: api/order
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<Order>>>> GetAllOrders(string Status = "Pending")
        {
            try
            {
                var orders = await _orderRepository.GetAllOrdersAsync(Status);
                return Ok(new ApiResponse<List<Order>>(orders, "Retrieved all orders successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<Order>>(HttpStatusCode.InternalServerError, "Internal server error: " + ex.Message));
            }
        }
        // POST: api/order
        [HttpPost]
        public async Task<ApiResponse<CreateOrderResponseDto>> CreateOrder([FromBody] OrderDto orderDto)
        {
            if (!ModelState.IsValid)
            {
                return new ApiResponse<CreateOrderResponseDto>(HttpStatusCode.BadRequest, "Invalid data", ModelState);
            }
            try
            {
                var response = await _orderRepository.CreateOrderAsync(orderDto);
                return new ApiResponse<CreateOrderResponseDto>(response, response.Message);
            }
            catch (Exception ex)
            {
                return new ApiResponse<CreateOrderResponseDto>(HttpStatusCode.InternalServerError, "Internal server error: " + ex.Message);
            }
        }
        // GET: api/order/5
        [HttpGet("{id}")]
        public async Task<ApiResponse<Order>> GetOrderById(int id)
        {
            try
            {
                var order = await _orderRepository.GetOrderDetailsAsync(id);
                if (order == null)
                {
                    return new ApiResponse<Order>(HttpStatusCode.NotFound, "Order not found.");
                }
                return new ApiResponse<Order>(order, "Order retrieved successfully.");
            }
            catch (Exception ex)
            {
                return new ApiResponse<Order>(HttpStatusCode.InternalServerError, "Internal server error: " + ex.Message);
            }
        }
        // PUT: api/order/5/status
        [HttpPut("{id}/status")]
        public async Task<ApiResponse<OrderStatusResponseDto>> UpdateOrderStatus(int id, [FromBody] OrderStatusDto status)
        {
            if (!ModelState.IsValid)
            {
                return new ApiResponse<OrderStatusResponseDto>(HttpStatusCode.BadRequest, "Invalid data", ModelState);
            }
            if (id != status.OrderId)
            {
                return new ApiResponse<OrderStatusResponseDto>(HttpStatusCode.BadRequest, "Mismatched Order ID");
            }
            try
            {
                var response = await _orderRepository.UpdateOrderStatusAsync(id, status.Status);
                return new ApiResponse<OrderStatusResponseDto>(response, response.Message);
            }
            catch (Exception ex)
            {
                return new ApiResponse<OrderStatusResponseDto>(HttpStatusCode.InternalServerError, "Internal server error: " + ex.Message);
            }
        }
        // PUT: api/order/5/confirm
        [HttpPut("{id}/confirm")]
        public async Task<ApiResponse<ConfirmOrderResponseDto>> ConfirmOrder(int id)
        {
            try
            {
                var response = await _orderRepository.ConfirmOrderAsync(id);
                return new ApiResponse<ConfirmOrderResponseDto>(response, response.Message);
            }
            catch (Exception ex)
            {
                return new ApiResponse<ConfirmOrderResponseDto>(HttpStatusCode.InternalServerError, "Internal server error: " + ex.Message);
            }
        }
    }
}
