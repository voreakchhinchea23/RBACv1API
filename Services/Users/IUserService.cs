using RbacApi.DTOs;

namespace RbacApi.Services.Users
{
    public interface IUserService
    {
        Task<IEnumerable<UserResponseDto>> GetUsersAsync();
        Task<UserResponseDto?> GetUserByIdAsync(int id);
        Task<UserResponseDto> CreateUserAsync(UserCreateDTO dto);
        Task<bool> UpdateUserAsync(UserUpdateDto dto, int id);
        Task<bool> DeleteUserAsync(int id);
    }
}