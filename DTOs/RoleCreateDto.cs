namespace RbacApi.DTOs
{
    public class RoleCreateDto
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
    }
}