namespace RbacApi.Models
{
    public class User
    {
        public int Id {get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public string? Phone { get; set; }   
        public bool IsActive { get; set; } = true;

        // FR to Role
        public int RoleId { get; set; }
        public Role? Role { get; set; }
    }
}