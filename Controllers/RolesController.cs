using Microsoft.AspNetCore.Mvc;
using RbacApi.DTOs;
using RbacApi.Services;

namespace RbacApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;
        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        // GET: api/roles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoleResponseDto>>> GetRoles()
        {
            var roles = await _roleService.GetRolesAsync();
            return Ok(roles);
        }

        // GET: api/roles/id
        [HttpGet("{id:int}")]
        public async Task<ActionResult<RoleResponseDto>> GetRoleById(int id)
        {
            var role = await _roleService.GetRoleByIdAsync(id);
            if (role == null) return NotFound(new { message = $"Role not found." });
            return Ok(role);
        }

        // POST: api/roles
        [HttpPost]
        public async Task<ActionResult<RoleResponseDto>> CreateRole(RoleCreateDto dto)
        {
            var createdRole = await _roleService.CreateRoleAsync(dto);
            return CreatedAtAction(nameof(GetRoles), new { id = createdRole.Id }, createdRole);
        }

        //PUT: api/roles/id
        [HttpPut("{id:int}")]
        public async Task<ActionResult> UpdateRole(RoleUpdateDto dto, int id)
        {
            var success = await _roleService.UpdateRoleAsync(dto, id);
            if (success == false) return NotFound(new { message = $"Role not found." });
            return NoContent();
        }

        // DELETE: api/roles/id
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteRole(int id)
        {
            var success = await _roleService.DeleteRoleAsync(id);
            if (success == false) return NotFound(new { message = $"Role not found." });
            return NoContent();
        }

        // POST: api/roles/assign-permissions
        [HttpPost("assign-permissions")]
        public async Task<ActionResult> AssignPermission(AssignPermissionsDto dto)
        {
            var success = await _roleService.AssignPermissionToRoleAsync(dto);
            if (!success)
            {
                return NotFound(new { message = "Role not found." });
            }

            return Ok(new { message = "Permissions assigned successfully." });
        }

        // POST api/roles/1/permissions/5
        [HttpPost("{roleId:int}/permissions/{permissionId:int}")]
        public async Task<ActionResult> AddPermissionToRole(int roleId, int permissionId)
        {
            var success = await _roleService.AddSinglePermissionAsync(roleId, permissionId);

            if (!success)
            {
                return NotFound(new { message = "Role or Permission not found." });
            }

            return NoContent();
        }

        // DELETE api/roles/1/permissions/5
        [HttpDelete("{roleId:int}/permissions/{permissionId:int}")]
        public async Task<ActionResult> RemovePermissionFromRole(int roleId, int permissionId)
        {
            var success = await _roleService.RemoveSinglePermissionAsync(roleId, permissionId);

            if (!success)
            {
                return NotFound(new { message = "Role Permission assignment not found." });
            }

            return NoContent();
        }
    }
}