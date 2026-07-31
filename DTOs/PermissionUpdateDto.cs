namespace RbacApi.DTOs
{
    public class PermissionUpdateDto
    {
        public required string Name { get; set; }
        public required string Module { get; set; }
        public string? Description { get; set; }
    }
}