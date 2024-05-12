using mm.api.Models;

namespace mm.api.Dtos
{
    public class RegisterResponseDto
    {
        public string Username { get; set; }
        public string Role { get; set; }
        public RegisterResponseDto(string username, string role)
        {
            Username = username;
            Role = role;
        }
    }
}
