using Microsoft.Data.SqlClient;
using mm.api.Data;
using mm.api.Dtos;
using mm.api.Models;
using mm.api.Repository.Interface;

namespace mm.api.Repository
{
    public class OrderRepository : IOrderRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;
        public OrderRepository(SqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }
        // Method to fetch orders based on Status
        public async Task<List<Order>> GetAllOrdersAsync(string Status)
        {
            var orders = new List<Order>();
            var query = "SELECT OrderId, CustomerId, TotalAmount, Status, OrderDate FROM Orders WHERE Status = @Status";
            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Status", Status);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var order = new Order
                            {
                                OrderId = reader.GetInt32(reader.GetOrdinal("OrderId")),
                                CustomerId = reader.GetInt32(reader.GetOrdinal("CustomerId")),
                                TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                                Status = reader.GetString(reader.GetOrdinal("Status")),
                                OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate"))
                            };
                            orders.Add(order);
                        }
                    }
                }
            }
            return orders;
        }
        //Create the Order with Pending State
        public async Task<CreateOrderResponseDto> CreateOrderAsync(OrderDto orderDto)
        {
            // Queries to fetch product details and to insert order and order items
            var productQuery = "SELECT ProductId, Price, Stock FROM Products WHERE ProductId = @ProductId AND IsDeleted = 0";
            var orderQuery = "INSERT INTO Orders (CustomerId, TotalAmount, Status, OrderDate) OUTPUT INSERTED.OrderId VALUES (@CustomerId, @TotalAmount, @Status, @OrderDate)";
            var itemQuery = "INSERT INTO OrderItems (OrderId, ProductId, Quantity, PriceAtOrder) VALUES (@OrderId, @ProductId, @Quantity, @PriceAtOrder)";
            decimal totalAmount = 0m;
            List<OrderItem> validatedItems = new List<OrderItem>();
            CreateOrderResponseDto createOrderResponseDTO = new CreateOrderResponseDto();
            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (OrderItemDto item in orderDto.Items)
                        {
                            using (var productCommand = new SqlCommand(productQuery, connection, transaction))
                            {
                                productCommand.Parameters.AddWithValue("@ProductId", item.ProductId);
                                using (var reader = await productCommand.ExecuteReaderAsync())
                                {
                                    if (await reader.ReadAsync())
                                    {
                                        int stockQuantity = reader.GetInt32(reader.GetOrdinal("Stock"));
                                        decimal price = reader.GetDecimal(reader.GetOrdinal("Price"));
                                        if (stockQuantity >= item.Quantity)
                                        {
                                            totalAmount += price * item.Quantity;
                                            validatedItems.Add(new OrderItem
                                            {
                                                ProductId = item.ProductId,
                                                Quantity = item.Quantity,
                                                PriceAtOrder = price  // Using the price from the database
                                            });
                                        }
                                        else
                                        {
                                            // Handle the case where there isn't enough stock
                                            createOrderResponseDTO.Message = $"Insufficient Stock for Product ID {item.ProductId}";
                                            createOrderResponseDTO.IsCreated = false;
                                            return createOrderResponseDTO;
                                        }
                                    }
                                    else
                                    {
                                        // Handle the case for Invalid Product Id
                                        createOrderResponseDTO.Message = $"Product Not Found for Product ID {item.ProductId}";
                                        createOrderResponseDTO.IsCreated = false;
                                        return createOrderResponseDTO;
                                    }
                                    reader.Close(); // Ensure the reader is closed before next iteration
                                }
                            }
                        }
                        // Proceed with creating the order if all items are validated
                        using (var orderCommand = new SqlCommand(orderQuery, connection, transaction))
                        {
                            orderCommand.Parameters.AddWithValue("@CustomerId", orderDto.CustomerId);
                            orderCommand.Parameters.AddWithValue("@TotalAmount", totalAmount);
                            orderCommand.Parameters.AddWithValue("@Status", "Pending");
                            orderCommand.Parameters.AddWithValue("@OrderDate", DateTime.Now);
                            var orderId = (int)await orderCommand.ExecuteScalarAsync();
                            // Insert all validated items
                            foreach (var validatedItem in validatedItems)
                            {
                                using (var itemCommand = new SqlCommand(itemQuery, connection, transaction))
                                {
                                    itemCommand.Parameters.AddWithValue("@OrderId", orderId);
                                    itemCommand.Parameters.AddWithValue("@ProductId", validatedItem.ProductId);
                                    itemCommand.Parameters.AddWithValue("@Quantity", validatedItem.Quantity);
                                    itemCommand.Parameters.AddWithValue("@PriceAtOrder", validatedItem.PriceAtOrder);
                                    await itemCommand.ExecuteNonQueryAsync();
                                }
                            }
                            transaction.Commit();
                            createOrderResponseDTO.Status = "Pending";
                            createOrderResponseDTO.IsCreated = true;
                            createOrderResponseDTO.OrderId = orderId;
                            createOrderResponseDTO.Message = "Order Created Successfully";
                            return createOrderResponseDTO;
                        }
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;  // Re-throw to handle the exception further up the call stack
                    }
                }
            }
        }
        public async Task<ConfirmOrderResponseDto> ConfirmOrderAsync(int orderId)
        {
            // Queries to fetch order and payment details
            var orderDetailsQuery = "SELECT TotalAmount FROM Orders WHERE OrderId = @OrderId";
            var paymentDetailsQuery = "SELECT Amount, Status FROM Payments WHERE OrderId = @OrderId";
            var updateOrderStatusQuery = "UPDATE Orders SET Status = 'Confirmed' WHERE OrderId = @OrderId";
            var getOrderItemsQuery = "SELECT ProductId, Quantity FROM OrderItems WHERE OrderId = @OrderId";
            var updateProductQuery = "UPDATE Products SET Stock = Stock - @Stock WHERE ProductId = @ProductId";
            ConfirmOrderResponseDto confirmOrderResponseDTO = new ConfirmOrderResponseDto()
            {
                OrderId = orderId,
            };
            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        decimal orderAmount = 0m;
                        decimal paymentAmount = 0m;
                        string paymentStatus = string.Empty;
                        // Retrieve order amount
                        using (var orderCommand = new SqlCommand(orderDetailsQuery, connection, transaction))
                        {
                            orderCommand.Parameters.AddWithValue("@OrderId", orderId);
                            using (var reader = await orderCommand.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    orderAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount"));
                                }
                                reader.Close();
                            }
                        }
                        // Retrieve payment details
                        using (var paymentCommand = new SqlCommand(paymentDetailsQuery, connection, transaction))
                        {
                            paymentCommand.Parameters.AddWithValue("@OrderId", orderId);
                            using (var reader = await paymentCommand.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    paymentAmount = reader.GetDecimal(reader.GetOrdinal("Amount"));
                                    paymentStatus = reader.GetString(reader.GetOrdinal("Status"));
                                }
                                reader.Close();
                            }
                        }
                        // Check if payment is complete and matches the order total
                        if (paymentStatus == "Completed" && paymentAmount == orderAmount)
                        {
                            // Update product quantities
                            using (var itemCommand = new SqlCommand(getOrderItemsQuery, connection, transaction))
                            {
                                itemCommand.Parameters.AddWithValue("@OrderId", orderId);
                                using (var reader = await itemCommand.ExecuteReaderAsync())
                                {
                                    while (reader.Read())
                                    {
                                        int productId = reader.GetInt32(reader.GetOrdinal("ProductId"));
                                        int quantity = reader.GetInt32(reader.GetOrdinal("Quantity"));
                                        using (var updateProductCommand = new SqlCommand(updateProductQuery, connection, transaction))
                                        {
                                            updateProductCommand.Parameters.AddWithValue("@ProductId", productId);
                                            updateProductCommand.Parameters.AddWithValue("@Stock", quantity);
                                            await updateProductCommand.ExecuteNonQueryAsync();
                                        }
                                    }
                                    reader.Close();
                                }
                            }
                            // Update order status to 'Confirmed'
                            using (var statusCommand = new SqlCommand(updateOrderStatusQuery, connection, transaction))
                            {
                                statusCommand.Parameters.AddWithValue("@OrderId", orderId);
                                await statusCommand.ExecuteNonQueryAsync();
                            }
                            transaction.Commit();
                            confirmOrderResponseDTO.IsConfirmed = true;
                            confirmOrderResponseDTO.Message = "Order Confirmed Successfully";
                            return confirmOrderResponseDTO;
                        }
                        else
                        {
                            transaction.Rollback();
                            confirmOrderResponseDTO.IsConfirmed = false;
                            confirmOrderResponseDTO.Message = "Cannot Confirm Order: Payment is either incomplete or does not match the order total.";
                            return confirmOrderResponseDTO;
                        }
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception("Error Confirming Order: " + ex.Message);
                    }
                }
            }
        }
        // Update the order status with conditions
        // An order cannot move directly from "Pending" to "Delivered".
        // An order can only be set to "Cancelled" if it is currently "Pending".
        // An order can be marked as "Processing" only if it's currently "Confirmed"
        public async Task<OrderStatusResponseDto> UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            OrderStatusResponseDto orderStatusDto = new OrderStatusResponseDto()
            {
                OrderId = orderId
            };
            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();
                try
                {
                    // Fetch the current status of the order
                    var currentStatusQuery = "SELECT Status FROM Orders WHERE OrderId = @OrderId";
                    string currentStatus;
                    using (var statusCommand = new SqlCommand(currentStatusQuery, connection))
                    {
                        statusCommand.Parameters.AddWithValue("@OrderId", orderId);
                        var result = await statusCommand.ExecuteScalarAsync();
                        if (result == null)
                        {
                            orderStatusDto.Message = "Order not found.";
                            orderStatusDto.IsUpdated = false;
                            return orderStatusDto;
                        }
                        currentStatus = result.ToString();
                    }
                    // Check if the status transition is valid
                    if (!IsValidStatusTransition(currentStatus, newStatus))
                    {
                        orderStatusDto.Message = $"Invalid status transition from {currentStatus} to {newStatus}.";
                        orderStatusDto.IsUpdated = false;
                        return orderStatusDto;
                    }
                    // Update the status if valid
                    var updateStatusQuery = "UPDATE Orders SET Status = @NewStatus WHERE OrderId = @OrderId";
                    using (var updateCommand = new SqlCommand(updateStatusQuery, connection))
                    {
                        updateCommand.Parameters.AddWithValue("@OrderId", orderId);
                        updateCommand.Parameters.AddWithValue("@NewStatus", newStatus);
                        int rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            orderStatusDto.Message = $"Order status updated to {newStatus}";
                            orderStatusDto.Status = newStatus;
                            orderStatusDto.IsUpdated = true;
                        }
                        else
                        {
                            orderStatusDto.IsUpdated = false;
                            orderStatusDto.Message = $"No order found with ID {orderId}";
                        }
                    }
                    return orderStatusDto;
                }
                catch (Exception ex)
                {
                    throw new Exception("Error updating order status: " + ex.Message, ex);
                }
            }
        }
        private bool IsValidStatusTransition(string currentStatus, string newStatus)
        {
            // Define valid status transitions
            switch (currentStatus)
            {
                case "Pending":
                    return newStatus == "Processing" || newStatus == "Cancelled";
                case "Confirmed":
                    return newStatus == "Processing";
                case "Processing":
                    return newStatus == "Delivered";
                case "Delivered":
                    // Delivered orders should not transition to any other status
                    return false;
                case "Cancelled":
                    // Cancelled orders should not transition to any other status
                    return false;
                default:
                    return false;
            }
        }
        //Get the Order Details by Id
        public async Task<Order?> GetOrderDetailsAsync(int orderId)
        {
            var query = "SELECT OrderId, CustomerId, TotalAmount, Status, OrderDate FROM Orders WHERE OrderId = @OrderId";
            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@OrderId", orderId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (!reader.Read()) return null;
                        return new Order
                        {
                            OrderId = reader.GetInt32(reader.GetOrdinal("OrderId")),
                            CustomerId = reader.GetInt32(reader.GetOrdinal("CustomerId")),
                            TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                            Status = reader.GetString(reader.GetOrdinal("Status")),
                            OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate"))
                        };
                    }
                }
            }
        }
    }
}
