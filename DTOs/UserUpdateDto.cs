namespace RbacApi.DTOs
{
    public class UserUpdateDto
    {
        public required string Username { get; set; }
        public required string Email { get; set; }
        public string? Phone { get; set; }
        public int? RoleId { get; set; }
        public List<int>? RoleIds { get; set; }
        public bool IsActive { get; set; }
    }
}