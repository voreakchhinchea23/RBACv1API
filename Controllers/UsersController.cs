using Microsoft.AspNetCore.Mvc;
using RbacApi.Authorization;
using RbacApi.DTOs;
using RbacApi.Services;
using System.Security.Claims;

namespace RbacApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [HasPermission(Perm.USER_READ)]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        // GET: api/users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetsUsers()
        {
            var users = await _userService.GetUsersAsync();
            return Ok(users);
        }

        // GET: api/users/id
        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserResponseDto>> GetUserById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);

            if (user == null)
            {
                return NotFound(new { message = $"User not found." });
            }

            return Ok(user);
        }

        // POST: api/users
        [HasPermission(Perm.USER_CREATE)]
        [HttpPost]
        public async Task<ActionResult<UserResponseDto>> CreateUser(UserCreateDTO dto)
        {
            var createdUser = await _userService.CreateUserAsync(dto);
            return CreatedAtAction(nameof(GetsUsers), new { id = createdUser.Id }, createdUser);
        }

        //PUT api/users/id
        [HasPermission(Perm.USER_UPDATE)]
        [HttpPut("{id:int}")]
        public async Task<ActionResult> UpdateUser(UserUpdateDto dto, int id)
        {
            var success = await _userService.UpdateUserAsync(dto, id);

            if (!success)
            {
                return NotFound(new { message = $"User not found." });
            }
            return NoContent();
        }

        // DELETE api/users/id
        [HasPermission(Perm.USER_DELETE)]
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            //Prevent self-deletion using JWT token claims
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(currentUserIdClaim, out int currentUserId) && currentUserId == id)
            {
                return BadRequest(new { message = "Action denied: You cannot delete your own active account." });
            }
            //Call service (catches the "Last Admin" error)
            try
            {
                var success = await _userService.DeleteUserAsync(id);
                if (!success)
                {
                    return NotFound(new { message = "User not found." });
                }
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                // Returns 400 Bad Request with the "Cannot delete last Admin" message
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}