using Microsoft.Data.SqlClient;
using mm.api.Data;
using mm.api.Dtos;
using mm.api.Models;
using mm.api.Repository.Interface;

namespace mm.api.Repository
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;

        public CustomerRepository(SqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }
        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            var customers = new List<Customer>();
            var query = "SELECT CustomerId, Name, Email, Address FROM Customers WHERE IsDeleted = 0";
            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            customers.Add(new Customer
                            {
                                CustomerId = reader.GetInt32(reader.GetOrdinal("CustomerId")),
                                Name = reader.GetString(reader.GetOrdinal("Name")),
                                Email = reader.GetString(reader.GetOrdinal("Email")),
                                Address = reader.GetString(reader.GetOrdinal("Address")),
                                IsDeleted = false
                            });
                        }
                    }
                }
            }
            return customers;
        }

        public async Task<Customer?> GetCustomerByIdAsync(int customerId)
        {
            var query = "SELECT CustomerId, Name, Email, Address FROM Customers WHERE CustomerId = @CustomerId AND IsDeleted = 0";
            Customer? customer = null;
            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CustomerId", customerId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            customer = new Customer
                            {
                                CustomerId = reader.GetInt32(reader.GetOrdinal("CustomerId")),
                                Name = reader.GetString(reader.GetOrdinal("Name")),
                                Email = reader.GetString(reader.GetOrdinal("Email")),
                                Address = reader.GetString(reader.GetOrdinal("Address")),
                                IsDeleted = false
                            };
                        }
                    }
                }
            }
            return customer;
        }

        public async Task<int> InsertCustomerAsync(CustomerDto customer)
        {
            var query = @"INSERT INTO Customers (Name, Email, Address, IsDeleted) 
                        VALUES (@Name, @Email, @Address, 0);
                        SELECT CAST(SCOPE_IDENTITY() as int);";
            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", customer.Name);
                    command.Parameters.AddWithValue("@Email", customer.Email);
                    command.Parameters.AddWithValue("@Address", customer.Address);
                    int customerId = (int)await command.ExecuteScalarAsync();
                    return customerId;
                }
            }
        }

        public async Task UpdateCustomerAsync(CustomerDto customer)
        {
            var query = "UPDATE Customers SET Name = @Name, Email = @Email, Address = @Address WHERE CustomerId = @CustomerId";
            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CustomerId", customer.CustomerId);
                    command.Parameters.AddWithValue("@Name", customer.Name);
                    command.Parameters.AddWithValue("@Email", customer.Email);
                    command.Parameters.AddWithValue("@Address", customer.Address);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task DeleteCustomerAsync(int customerId)
        {
            var query = "UPDATE Customers SET IsDeleted = 1 WHERE CustomerId = @CustomerId";
            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CustomerId", customerId);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
