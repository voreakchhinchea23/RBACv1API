namespace RbacApi.DTOs
{
    public class UserResponseDto
    {
        public int Id { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
        public List<string> Roles { get; set; } = new();
        public required string RoleName { get; set; }
    }
}