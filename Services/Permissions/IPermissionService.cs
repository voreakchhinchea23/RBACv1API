using RbacApi.DTOs;
namespace RbacApi.Services
{
    public interface IPermissionService
    {
        Task<IEnumerable<PermissionResponseDto>> GetPermissionsAsync();
        Task<PermissionResponseDto?> GetPermissionByIdAsync(int id);
        Task<PermissionResponseDto> CreatePermissionAsync(PermissionCreateDto dto);
        Task<bool> UpdatePermissionAsync(PermissionUpdateDto dto, int id);
        Task<bool> DeletePermissionAsync(int id);
    }
}