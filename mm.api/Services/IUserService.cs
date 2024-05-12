using mm.api.Dtos;
using mm.api.Models;

namespace mm.api.Services
{
    public interface IUserService
    {
        Task<bool> AdminExistsAsync();
        Task<RegisterResponseDto> Register(User user, string password);
        Task<string> Login(UserLoginDto loginDto);
        Task RegisterAdmin();
    }
}
