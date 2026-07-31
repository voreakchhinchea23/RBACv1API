namespace RbacApi.Models
{
    public class Role
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }

        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }

    
}