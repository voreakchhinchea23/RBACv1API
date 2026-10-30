namespace RbacApi.DTOs
{
    public class AssignUserRolesDto
    {
        public int UserId { get; set; }
        public List<int> RoleIds { get; set; } = new();
    }
}
