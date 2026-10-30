using RbacApi.DTOs;

namespace RbacApi.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserResponseDto>> GetUsersAsync();
        Task<UserResponseDto?> GetUserByIdAsync(int id);
        Task<UserResponseDto> CreateUserAsync(UserCreateDTO dto);
        Task<bool> UpdateUserAsync(UserUpdateDto dto, int id);
        Task<bool> DeleteUserAsync(int id);
        Task<bool> AssignRolesToUserAsync(AssignUserRolesDto dto);
        Task<bool> AddSingleRoleAsync(int userId, int roleId);
        Task<bool> RemoveSingleRoleAsync(int userId, int roleId);
    }
}