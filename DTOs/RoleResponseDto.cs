namespace RbacApi.DTOs
{
    public class RoleResponseDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }

        // Show the permissions of a role
        public List<PermissionResponseDto> Permissions { get; set; } = new();
    }
}