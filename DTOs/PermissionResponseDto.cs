namespace RbacApi.DTOs
{
    public class PermissionResponseDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Module { get; set; }
        public string? Description { get; set; }
    }
}