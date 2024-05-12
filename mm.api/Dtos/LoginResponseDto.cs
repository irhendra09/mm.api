namespace mm.api.Dtos
{
    public class LoginResponseDto
    {
        public string Username { get; set; }
        public string Role { get; set; }
        public string Token { get; set; }
        public LoginResponseDto(string username, string role, string token)
        {
            Username = username;
            Role = role;
            Token = token;
        }
    }
}
