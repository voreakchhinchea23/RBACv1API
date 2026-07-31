namespace RbacApi.DTOs
{
    public class AssignPermissionsDto
    {
        public int RoleId { get; set; }
        public required List<int> PermissionIds { get; set; }
    }
}