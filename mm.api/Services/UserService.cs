using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using mm.api.Data;
using mm.api.Dtos;
using mm.api.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace mm.api.Services
{
    public class UserService : IUserService
    {
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly string _pepper;
        private readonly int _iteration = 3;

        public UserService(SqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
            _pepper = Environment.GetEnvironmentVariable("PasswordHashExamplePepper");
        }

        public async Task<bool> AdminExistsAsync()
        {
            const string query = "SELECT COUNT(*) FROM Users WHERE Role = 'Admin'";

            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(query, connection))
                {
                    var result = await command.ExecuteScalarAsync();
                    return (int)result > 0;
                }
            }
        }

        public async Task<string> Login(UserLoginDto loginDto)
        {
            const string query = @"SELECT Username, PasswordSalt, PasswordHash, Role FROM Users WHERE Username = @Username";

            try
            {
                using (var connection = _connectionFactory.CreateConnection())
                {
                    await connection.OpenAsync().ConfigureAwait(false);

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", loginDto.Username);

                        using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                        {
                            if (!await reader.ReadAsync().ConfigureAwait(false))
                                throw new Exception("Username or password did not match.");

                            var username = reader.GetString(reader.GetOrdinal("Username"));
                            var passwordSalt = reader.GetString(reader.GetOrdinal("PasswordSalt"));
                            var passwordHash = reader.GetString(reader.GetOrdinal("PasswordHash"));
                            var role = reader.GetString(reader.GetOrdinal("Role"));

                            var computedHash = PasswordHasher.ComputeHash(loginDto.Password, passwordSalt, _pepper, _iteration);
                            if (passwordHash != computedHash)
                                throw new Exception("Username or password did not match.");


                            var tokenHandler = new JwtSecurityTokenHandler();
                            var key = Encoding.ASCII.GetBytes("qkEnyu+RHAp3rdoY+ZIJzzttZjs9Crvk6IDrm8y9lxY=");
                            var tokenDescriptor = new SecurityTokenDescriptor
                            {
                                Subject = new ClaimsIdentity(new Claim[]
                                {
                                     new Claim(ClaimTypes.Name, username),
                                     new Claim(ClaimTypes.Role, role)
 
                                }),
                                Expires = DateTime.UtcNow.AddDays(1),
                                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                            };
                            var token = tokenHandler.CreateToken(tokenDescriptor);
                            var tokenString = tokenHandler.WriteToken(token);

                            return tokenString;
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }


        public async Task<RegisterResponseDto> Register(User user, string password)
        {
            const string role = "User";

            var query = @"INSERT INTO Users (Username, PasswordSalt, PasswordHash, Role) VALUES (@Username, @PasswordSalt, @PasswordHash, @Role)";

            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(query, connection))
                {
                    var salt = PasswordHasher.GenerateSalt();
                    var hashedPassword = PasswordHasher.ComputeHash(password, salt, _pepper, _iteration);

                    command.Parameters.AddWithValue("@Username", user.Username);
                    command.Parameters.AddWithValue("@PasswordSalt", salt);
                    command.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                    command.Parameters.AddWithValue("@Role", role);

                    await command.ExecuteNonQueryAsync();
                }
            }

            return new RegisterResponseDto(user.Username, role);
        }
        public async Task RegisterAdmin()
        {
            var adminExists = await AdminExistsAsync();

            if (!adminExists)
            {
                const string role = "Admin";
                const string Username = "admin";
                const string Password = "admin123";

                var query = @"INSERT INTO Users (Username, PasswordSalt, PasswordHash, Role) VALUES (@Username, @PasswordSalt, @PasswordHash, @Role)";

                using (var connection = _connectionFactory.CreateConnection())
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(query, connection))
                    {
                        var salt = PasswordHasher.GenerateSalt();
                        var hashedPassword = PasswordHasher.ComputeHash(Password, salt, _pepper, _iteration);

                        command.Parameters.AddWithValue("@Username", Username);
                        command.Parameters.AddWithValue("@PasswordSalt", salt);
                        command.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                        command.Parameters.AddWithValue("@Role", role);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
        }
    }
}
