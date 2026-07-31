namespace RbacApi.DTOs
{
    public class RoleUpdateDto
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
    }
}