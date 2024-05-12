using Microsoft.Data.SqlClient;
using mm.api.Data;
using mm.api.Dtos;
using mm.api.Models;
using mm.api.Repository.Interface;

namespace mm.api.Repository
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;
        public PaymentRepository(SqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }
        public async Task<PaymentResponseDto> MakePaymentAsync(PaymentDto paymentDto)
        {
            var orderValidationQuery = "SELECT TotalAmount FROM Orders WHERE OrderId = @OrderId AND Status = 'Pending'";
            var insertPaymentQuery = "INSERT INTO Payments (OrderId, Amount, Status, PaymentType, PaymentDate) OUTPUT INSERTED.PaymentId VALUES (@OrderId, @Amount, 'Pending', @PaymentType, @PaymentDate)";
            var updatePaymentStatusQuery = "UPDATE Payments SET Status = @Status WHERE PaymentId = @PaymentId";
            PaymentResponseDto paymentResponseDTO = new PaymentResponseDto();
            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {

                        decimal orderAmount = 0m;
                        using (var validationCommand = new SqlCommand(orderValidationQuery, connection, transaction))
                        {
                            validationCommand.Parameters.AddWithValue("@OrderId", paymentDto.OrderId);
                            var result = await validationCommand.ExecuteScalarAsync();
                            if (result == null)
                            {
                                paymentResponseDTO.Message = "Order either does not exist or is not in a pending state.";
                                return paymentResponseDTO;
                            }
                            orderAmount = (decimal)result;
                        }
                        if (orderAmount != paymentDto.Amount)
                        {
                            paymentResponseDTO.Message = "Payment amount does not match the order total.";
                            return paymentResponseDTO;
                        }

                        int paymentId;
                        using (var insertCommand = new SqlCommand(insertPaymentQuery, connection, transaction))
                        {
                            insertCommand.Parameters.AddWithValue("@OrderId", paymentDto.OrderId);
                            insertCommand.Parameters.AddWithValue("@Amount", paymentDto.Amount);
                            insertCommand.Parameters.AddWithValue("@PaymentType", paymentDto.PaymentType);
                            insertCommand.Parameters.AddWithValue("@PaymentDate", DateTime.Now);
                            paymentId = (int)await insertCommand.ExecuteScalarAsync();
                        }

                        string paymentStatus = SimulatePaymentGatewayInteraction(paymentDto);

                        using (var updateCommand = new SqlCommand(updatePaymentStatusQuery, connection, transaction))
                        {
                            updateCommand.Parameters.AddWithValue("@Status", paymentStatus);
                            updateCommand.Parameters.AddWithValue("@PaymentId", paymentId);
                            await updateCommand.ExecuteNonQueryAsync();
                            paymentResponseDTO.IsCreated = true;
                            paymentResponseDTO.Status = paymentStatus;
                            paymentResponseDTO.PaymentId = paymentId;
                            paymentResponseDTO.Message = $"Payment Processed with Status {paymentStatus}";
                        }
                        transaction.Commit();
                        return paymentResponseDTO;
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
        private string SimulatePaymentGatewayInteraction(PaymentDto paymentDto)
        {

            switch (paymentDto.PaymentType)
            {
                case "COD":
                    return "Completed"; 
                case "CC":
                    return "Completed";
                case "DC":
                    return "Failed";    
                default:
                    return "Completed"; 
            }
        }
        public async Task<UpdatePaymentResponseDto> UpdatePaymentStatusAsync(int paymentId, string newStatus)
        {
            
            var paymentDetailsQuery = "SELECT p.OrderId, p.Amount, p.Status, o.Status AS OrderStatus FROM Payments p INNER JOIN Orders o ON p.OrderId = o.OrderId WHERE p.PaymentId = @PaymentId";
            var updatePaymentStatusQuery = "UPDATE Payments SET Status = @Status WHERE PaymentId = @PaymentId";
            UpdatePaymentResponseDto updatePaymentResponseDTO = new UpdatePaymentResponseDto()
            {
                PaymentId = paymentId
            };
            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();
                int orderId;
                decimal paymentAmount;
                string currentPaymentStatus, orderStatus;

                using (var command = new SqlCommand(paymentDetailsQuery, connection))
                {
                    command.Parameters.AddWithValue("@PaymentId", paymentId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (!reader.Read())
                        {
                            throw new Exception("Payment record not found.");
                        }
                        orderId = reader.GetInt32(reader.GetOrdinal("OrderId"));
                        paymentAmount = reader.GetDecimal(reader.GetOrdinal("Amount"));
                        currentPaymentStatus = reader.GetString(reader.GetOrdinal("Status"));
                        orderStatus = reader.GetString(reader.GetOrdinal("OrderStatus"));

                        updatePaymentResponseDTO.CurrentStatus = currentPaymentStatus;
                    }
                }

                if (!IsValidStatusTransition(currentPaymentStatus, newStatus, orderStatus))
                {
                    updatePaymentResponseDTO.IsUpdated = false;
                    updatePaymentResponseDTO.Message = $"Invalid status transition from {currentPaymentStatus} to {newStatus} for order status {orderStatus}.";
                    return updatePaymentResponseDTO;
                }

                using (var updateCommand = new SqlCommand(updatePaymentStatusQuery, connection))
                {
                    updateCommand.Parameters.AddWithValue("@PaymentId", paymentId);
                    updateCommand.Parameters.AddWithValue("@Status", newStatus);
                    await updateCommand.ExecuteNonQueryAsync();
                    updatePaymentResponseDTO.IsUpdated = true;
                    updatePaymentResponseDTO.UpdatedStatus = newStatus;
                    updatePaymentResponseDTO.Message = $"Payment Status Updated from {currentPaymentStatus} to {newStatus}";
                    return updatePaymentResponseDTO;
                }
            }
        }
        public async Task<Payment?> GetPaymentDetailsAsync(int paymentId)
        {
            var query = "SELECT PaymentId, OrderId, Amount, Status, PaymentType, PaymentDate FROM Payments WHERE PaymentId = @PaymentId";
            Payment? payment = null;
            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PaymentId", paymentId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            payment = new Payment
                            {
                                PaymentId = reader.GetInt32(reader.GetOrdinal("PaymentId")),
                                OrderId = reader.GetInt32(reader.GetOrdinal("OrderId")),
                                Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                                Status = reader.GetString(reader.GetOrdinal("Status")),
                                PaymentType = reader.GetString(reader.GetOrdinal("PaymentType")),
                                PaymentDate = reader.GetDateTime(reader.GetOrdinal("PaymentDate"))
                            };
                        }
                    }
                }
            }
            return payment;
        }
        private bool IsValidStatusTransition(string currentStatus, string newStatus, string orderStatus)
        {

            if (currentStatus == "Completed" && newStatus != "Refund")
            {
                return false;
            }

            if (currentStatus == "Pending" && newStatus == "Cancelled")
            {
                return true;
            }

            if (currentStatus == "Completed" && newStatus == "Refund" && orderStatus != "Returned")
            {
                return false;
            }

            if (newStatus == "Failed" && (currentStatus == "Completed" || currentStatus == "Cancelled"))
            {
                return false;
            }

            if (currentStatus == "Pending" && newStatus == "Completed" && (orderStatus == "Shipped" || orderStatus == "Confirmed"))
            {
                return true;
            }

            return true;
        }
    }
}
