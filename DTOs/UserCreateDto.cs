namespace RbacApi.DTOs
{
    public class UserCreateDTO
    {
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; } // Raw password 
        public string? Phone { get; set; }
        public int RoleId { get; set; }

       
    }
}