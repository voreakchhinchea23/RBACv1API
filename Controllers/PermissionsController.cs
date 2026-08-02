using Microsoft.AspNetCore.Mvc;
using RbacApi.DTOs;
using RbacApi.Services;

namespace RbacApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PermissionsController : ControllerBase
    {
        private readonly IPermissionService _permissionService;
        public PermissionsController(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }


        // GET: api/permissions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PermissionResponseDto>>> GetPermissions()
        {
            var perm = await _permissionService.GetPermissionsAsync();
            return Ok(perm);
        }

        // GET: api/permissions/id
        [HttpGet("{id:int}")]
        public async Task<ActionResult<PermissionResponseDto>> GetPermissionById(int id)
        {
            var perm = await _permissionService.GetPermissionByIdAsync(id);
            if (perm == null) return NotFound(new {message = $"Permission not found."});
            return Ok(perm);
        }

        //POST: api/permissions
        [HttpPost]
        public async Task<ActionResult<PermissionResponseDto>> CreatePermission(PermissionCreateDto dto)
        {
            var perm = await _permissionService.CreatePermissionAsync(dto);
            return CreatedAtAction(nameof(GetPermissions), new{id = perm.Id}, perm);
        }

        //PUT: api/permissions/id
        [HttpPut("{id:int}")]
        public async Task<ActionResult> UpdatePermission(PermissionUpdateDto dto, int id)
        {
            var success = await _permissionService.UpdatePermissionAsync(dto, id);
            if (success == false) return NotFound(new {message = "Permission not found."});
            return NoContent();
        }

        //DELETE: api/permissions/id
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeletePermission(int id)
        {
            var success = await _permissionService.DeletePermissionAsync(id);
            if (success == false) return NotFound(new {message = "Permission not found."});
            return NoContent();
        }
    }
}