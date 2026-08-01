using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RbacApi.Data;
using RbacApi.DTOs;
using RbacApi.Models;
using RbacApi.Services.Users;

namespace RbacApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
        [HttpPost]
        public async Task<ActionResult<UserResponseDto>> CreateUser(UserCreateDTO dto)
        {
            var createdUser = await _userService.CreateUserAsync(dto);
            return CreatedAtAction(nameof(GetsUsers), new { id = createdUser.Id }, createdUser);
        }

        //PUT api/users/id
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
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            var success = await _userService.DeleteUserAsync(id);
            if (!success)
            {
                return NotFound(new { message = $"User not found." });
            }
            return NoContent();
        }
    }
}