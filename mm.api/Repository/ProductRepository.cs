using Microsoft.Data.SqlClient;
using mm.api.Data;
using mm.api.Dtos;
using mm.api.Models;
using mm.api.Repository.Interface;

namespace mm.api.Repository
{
    public class ProductRepository : IProductRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;
        public ProductRepository(SqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }
        public async Task<List<Product>> GetAllProductsAsync()
        {
            var products = new List<Product>();
            var query = "SELECT ProductId, Name, Price, Stock, IsDeleted FROM Products WHERE IsDeleted = 0";
            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            products.Add(new Product
                            {
                                ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                                Name = reader.GetString(reader.GetOrdinal("Name")),
                                Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                                Stock = reader.GetInt32(reader.GetOrdinal("Stock")),
                                IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted"))
                            });
                        }
                    }
                }
            }
            return products;
        }
        public async Task<Product?> GetProductByIdAsync(int productId)
        {
            var query = "SELECT ProductId, Name, Price, Stock, IsDeleted FROM Products WHERE ProductId = @ProductId AND IsDeleted = 0";
            Product? product = null;
            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ProductId", productId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            product = new Product
                            {
                                ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                                Name = reader.GetString(reader.GetOrdinal("Name")),
                                Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                                Stock = reader.GetInt32(reader.GetOrdinal("Stock")),
                                IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted"))
                            };
                        }
                    }
                }
            }
            return product;
        }
        public async Task<int> InsertProductAsync(ProductDto product)
        {
            var query = @"INSERT INTO Products (Name, Price, Stock, IsDeleted) 
                        VALUES (@Name, @Price, @Stock, 0);
                        SELECT CAST(SCOPE_IDENTITY() as int);";
            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", product.Name);
                    command.Parameters.AddWithValue("@Price", product.Price);
                    command.Parameters.AddWithValue("@Stock", product.Stock);
                    int productId = (int)await command.ExecuteScalarAsync();
                    return productId;
                }
            }
        }
        public async Task UpdateProductAsync(ProductDto product)
        {
            var query = "UPDATE Products SET Name = @Name, Price = @Price, Stock = @Stock, Description = @Description WHERE ProductId = @ProductId";
            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ProductId", product.ProductId);
                    command.Parameters.AddWithValue("@Name", product.Name);
                    command.Parameters.AddWithValue("@Price", product.Price);
                    command.Parameters.AddWithValue("@Stock", product.Stock);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        public async Task DeleteProductAsync(int productId)
        {
            var query = "UPDATE Products SET IsDeleted = 1 WHERE ProductId = @ProductId";
            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ProductId", productId);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
