using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RbacApi.Data;
using RbacApi.DTOs;
using RbacApi.Controllers;
using RbacApi.Models;
using RbacApi.Services;
using System.Security.Claims;
using Xunit;

namespace RbacApi.Tests
{
    public class UserControllerTests
    {
        private AppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        private void SetUserContext(ControllerBase controller, int userId, string username, string role = "Admin")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        [Fact]
        public async Task UsersController_CRUD_And_RoleAssignments()
        {
            using var context = CreateInMemoryDbContext();
            await DatabaseSeeder.SeedAsync(context);

            var userService = new UserService(context);
            var controller = new UsersController(userService);
            SetUserContext(controller, 999, "super_admin");

            // 1. GetUsers
            var listResult = await controller.GetsUsers();
            var okList = Assert.IsType<OkObjectResult>(listResult.Result);
            var users = Assert.IsAssignableFrom<IEnumerable<UserResponseDto>>(okList.Value);
            Assert.NotEmpty(users);

            // 2. CreateUser
            var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");
            var managerRole = await context.Roles.FirstAsync(r => r.Name == "Manager");

            var createResult = await controller.CreateUser(new UserCreateDTO
            {
                Username = "alice",
                Email = "alice@example.com",
                Password = "Password123!",
                RoleIds = new List<int> { managerRole.Id }
            });

            var createdAction = Assert.IsType<CreatedAtActionResult>(createResult.Result);
            var createdDto = Assert.IsType<UserResponseDto>(createdAction.Value);
            Assert.Equal("alice", createdDto.Username);
            Assert.Contains("Manager", createdDto.Roles);

            // 3. AssignRoles (multi-role)
            var assignResult = await controller.AssignRoles(new AssignUserRolesDto
            {
                UserId = createdDto.Id,
                RoleIds = new List<int> { adminRole.Id, managerRole.Id }
            });
            Assert.IsType<OkObjectResult>(assignResult);

            var updatedUser = await userService.GetUserByIdAsync(createdDto.Id);
            Assert.NotNull(updatedUser);
            Assert.Equal(2, updatedUser.Roles.Count);
            Assert.Contains("Admin", updatedUser.Roles);
            Assert.Contains("Manager", updatedUser.Roles);

            // 4. RemoveRoleFromUser
            var removeResult = await controller.RemoveRoleFromUser(createdDto.Id, adminRole.Id);
            Assert.IsType<NoContentResult>(removeResult);

            var userAfterRemove = await userService.GetUserByIdAsync(createdDto.Id);
            Assert.NotNull(userAfterRemove);
            Assert.Single(userAfterRemove.Roles);
            Assert.DoesNotContain("Admin", userAfterRemove.Roles);

            // 5. AddRoleToUser
            var addResult = await controller.AddRoleToUser(createdDto.Id, adminRole.Id);
            Assert.IsType<NoContentResult>(addResult);

            var userAfterAdd = await userService.GetUserByIdAsync(createdDto.Id);
            Assert.NotNull(userAfterAdd);
            Assert.Equal(2, userAfterAdd.Roles.Count);

            // 6. Prevent self-deletion
            SetUserContext(controller, createdDto.Id, "alice");
            var selfDeleteResult = await controller.DeleteUser(createdDto.Id);
            var badRequest = Assert.IsType<BadRequestObjectResult>(selfDeleteResult);
            Assert.Contains("cannot delete your own active account", badRequest.Value!.ToString(), StringComparison.OrdinalIgnoreCase);

            // 7. Delete allowed from another user
            SetUserContext(controller, 999, "super_admin");
            var deleteResult = await controller.DeleteUser(createdDto.Id);
            Assert.IsType<NoContentResult>(deleteResult);
        }

        [Fact]
        public async Task RolesController_CreateRole_ReturnsCreatedAtActionWithGetRoleById()
        {
            using var context = CreateInMemoryDbContext();
            await DatabaseSeeder.SeedAsync(context);

            var roleService = new RoleService(context);
            var controller = new RolesController(roleService);
            SetUserContext(controller, 999, "super_admin");

            var result = await controller.CreateRole(new RoleCreateDto
            {
                Name = "Supervisor",
                Description = "Branch supervisor"
            });

            var createdAction = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(RolesController.GetRoleById), createdAction.ActionName);
            var value = Assert.IsType<RoleResponseDto>(createdAction.Value);
            Assert.Equal("Supervisor", value.Name);
        }

        [Fact]
        public async Task RolesController_DeleteAdminRole_ReturnsBadRequest()
        {
            using var context = CreateInMemoryDbContext();
            await DatabaseSeeder.SeedAsync(context);

            var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");
            var roleService = new RoleService(context);
            var controller = new RolesController(roleService);
            SetUserContext(controller, 999, "super_admin");

            var result = await controller.DeleteRole(adminRole.Id);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Cannot delete the core Administrator role", badRequest.Value!.ToString());
        }

        [Fact]
        public async Task PermissionsController_CreatePermission_ReturnsCreatedAtActionWithGetPermissionById()
        {
            using var context = CreateInMemoryDbContext();
            await DatabaseSeeder.SeedAsync(context);

            var permService = new PermissionService(context);
            var controller = new PermissionsController(permService);
            SetUserContext(controller, 999, "super_admin");

            var result = await controller.CreatePermission(new PermissionCreateDto
            {
                Name = "custom.action",
                Module = "CUSTOM",
                Description = "Custom permission"
            });

            var createdAction = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(PermissionsController.GetPermissionById), createdAction.ActionName);
            var value = Assert.IsType<PermissionResponseDto>(createdAction.Value);
            Assert.Equal("custom.action", value.Name);
        }
    }
}
