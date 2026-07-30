namespace Rbac.Models
{
    public class Permission
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Module { get; set; }
        public string Description { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}