using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RbacApi.Authorization;
using RbacApi.Data;
using RbacApi.DTOs;
using RbacApi.Models;
using RbacApi.Services;
using RbacApi.Services.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace RbacApi.Tests
{
    public class RbacTests
    {
        private AppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        private IConfiguration CreateTestConfiguration()
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "Jwt:Key", "ThisIsMyVerySecureSecretKeyForJwtToken12345!" },
                { "Jwt:Issuer", "RbacApi" },
                { "Jwt:Audience", "RbacApi" }
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        [Fact]
        public async Task DatabaseSeeder_SeedsRolesPermissionsAndAdminUserWithUserRole()
        {
            using var context = CreateInMemoryDbContext();

            await DatabaseSeeder.SeedAsync(context);

            // Assert roles seeded
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            var managerRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Manager");
            Assert.NotNull(adminRole);
            Assert.NotNull(managerRole);

            // Assert permissions seeded
            var allPermCodes = Perm.GetAll();
            var dbPermCount = await context.Permissions.CountAsync();
            Assert.Equal(allPermCodes.Count, dbPermCount);

            // Assert Admin has all permissions in RolePermissions
            var adminPermCount = await context.RolePermissions.CountAsync(rp => rp.RoleId == adminRole.Id);
            Assert.Equal(allPermCodes.Count, adminPermCount);

            // Assert Admin user seeded
            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
            Assert.NotNull(adminUser);

            // Assert UserRole links adminUser to adminRole
            var userRole = await context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRole.Id);
            Assert.NotNull(userRole);
        }

        [Fact]
        public async Task AuthService_Login_ReturnsTokenWithRolesAndPermissions()
        {
            using var context = CreateInMemoryDbContext();
            var config = CreateTestConfiguration();

            await DatabaseSeeder.SeedAsync(context);

            var authService = new AuthService(context, config);

            var loginResult = await authService.LoginAsync(new LoginRequestDto
            {
                Username = "admin",
                Password = "Abc123$"
            });

            Assert.NotNull(loginResult);
            Assert.NotEmpty(loginResult.Token);
            Assert.Equal("admin", loginResult.Username);
            Assert.Contains("Admin", loginResult.Roles);
            Assert.Equal("Admin", loginResult.Role);
            Assert.Contains("user.read", loginResult.Permissions);
            Assert.Contains("role.read", loginResult.Permissions);

            // Decode token and verify claims
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(loginResult.Token);

            var roleClaims = jwtToken.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            Assert.Contains("Admin", roleClaims);

            var permClaims = jwtToken.Claims.Where(c => c.Type == "permission").Select(c => c.Value).ToList();
            Assert.Contains("user.read", permClaims);
            Assert.Contains("role.create", permClaims);
        }

        [Fact]
        public async Task UserService_CreateUser_SupportsMultipleRoles()
        {
            using var context = CreateInMemoryDbContext();
            await DatabaseSeeder.SeedAsync(context);

            var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");
            var managerRole = await context.Roles.FirstAsync(r => r.Name == "Manager");

            var userService = new UserService(context);

            var newUserDto = new UserCreateDTO
            {
                Username = "john_doe",
                Email = "john@example.com",
                Password = "Password123!",
                Phone = "1234567890",
                RoleIds = new List<int> { adminRole.Id, managerRole.Id }
            };

            var created = await userService.CreateUserAsync(newUserDto);

            Assert.NotNull(created);
            Assert.Equal(2, created.Roles.Count);
            Assert.Contains("Admin", created.Roles);
            Assert.Contains("Manager", created.Roles);

            var dbUserRoles = await context.UserRoles.Where(ur => ur.UserId == created.Id).ToListAsync();
            Assert.Equal(2, dbUserRoles.Count);
        }

        [Fact]
        public async Task UserService_AssignRolesToUser_ReplacesRolesCorrectly()
        {
            using var context = CreateInMemoryDbContext();
            await DatabaseSeeder.SeedAsync(context);

            var managerRole = await context.Roles.FirstAsync(r => r.Name == "Manager");
            var userService = new UserService(context);

            var created = await userService.CreateUserAsync(new UserCreateDTO
            {
                Username = "staff_member",
                Email = "staff@example.com",
                Password = "Password123!",
                RoleId = managerRole.Id
            });

            Assert.Single(created.Roles);
            Assert.Contains("Manager", created.Roles);

            // Create a custom role
            var supportRole = new Role { Name = "Support" };
            context.Roles.Add(supportRole);
            await context.SaveChangesAsync();

            // Reassign to Support role only
            var assigned = await userService.AssignRolesToUserAsync(new AssignUserRolesDto
            {
                UserId = created.Id,
                RoleIds = new List<int> { supportRole.Id }
            });

            Assert.True(assigned);

            var updatedUser = await userService.GetUserByIdAsync(created.Id);
            Assert.NotNull(updatedUser);
            Assert.Single(updatedUser.Roles);
            Assert.Contains("Support", updatedUser.Roles);
            Assert.DoesNotContain("Manager", updatedUser.Roles);
        }

        [Fact]
        public async Task UserService_AddAndRemoveSingleRole_WorksProperly()
        {
            using var context = CreateInMemoryDbContext();
            await DatabaseSeeder.SeedAsync(context);

            var managerRole = await context.Roles.FirstAsync(r => r.Name == "Manager");
            var userService = new UserService(context);

            var created = await userService.CreateUserAsync(new UserCreateDTO
            {
                Username = "test_user",
                Email = "test@example.com",
                Password = "Password123!"
            });

            Assert.Empty(created.Roles);

            // Add single role
            var added = await userService.AddSingleRoleAsync(created.Id, managerRole.Id);
            Assert.True(added);

            var userAfterAdd = await userService.GetUserByIdAsync(created.Id);
            Assert.NotNull(userAfterAdd);
            Assert.Contains("Manager", userAfterAdd.Roles);

            // Remove single role
            var removed = await userService.RemoveSingleRoleAsync(created.Id, managerRole.Id);
            Assert.True(removed);

            var userAfterRemove = await userService.GetUserByIdAsync(created.Id);
            Assert.NotNull(userAfterRemove);
            Assert.Empty(userAfterRemove.Roles);
        }

        [Fact]
        public async Task UserService_DeleteUser_PreventsDeletingLastAdmin()
        {
            using var context = CreateInMemoryDbContext();
            await DatabaseSeeder.SeedAsync(context);

            var adminUser = await context.Users.FirstAsync(u => u.Username == "admin");
            var userService = new UserService(context);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => userService.DeleteUserAsync(adminUser.Id));
            Assert.Equal("Action denied: Cannot delete the last remaining Administrator account.", ex.Message);
        }

        [Fact]
        public async Task UserService_RemoveSingleRole_PreventsRemovingAdminFromLastAdmin()
        {
            using var context = CreateInMemoryDbContext();
            await DatabaseSeeder.SeedAsync(context);

            var adminUser = await context.Users.FirstAsync(u => u.Username == "admin");
            var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");
            var userService = new UserService(context);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => userService.RemoveSingleRoleAsync(adminUser.Id, adminRole.Id));
            Assert.Equal("Action denied: Cannot remove Admin role from the last remaining Administrator account.", ex.Message);
        }

        [Fact]
        public async Task UserService_UpdateUser_PreventsRemovingAdminRoleFromLastAdmin()
        {
            using var context = CreateInMemoryDbContext();
            await DatabaseSeeder.SeedAsync(context);

            var adminUser = await context.Users.FirstAsync(u => u.Username == "admin");
            var managerRole = await context.Roles.FirstAsync(r => r.Name == "Manager");
            var userService = new UserService(context);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => userService.UpdateUserAsync(new UserUpdateDto
            {
                Username = "admin",
                Email = "admin@rbac.local",
                RoleIds = new List<int> { managerRole.Id },
                IsActive = true
            }, adminUser.Id));

            Assert.Equal("Action denied: Cannot remove Admin role from the last remaining Administrator account.", ex.Message);
        }

        [Fact]
        public async Task UserService_UpdateUser_PreventsDeactivatingLastActiveAdmin()
        {
            using var context = CreateInMemoryDbContext();
            await DatabaseSeeder.SeedAsync(context);

            var adminUser = await context.Users.FirstAsync(u => u.Username == "admin");
            var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");
            var userService = new UserService(context);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => userService.UpdateUserAsync(new UserUpdateDto
            {
                Username = "admin",
                Email = "admin@rbac.local",
                RoleIds = new List<int> { adminRole.Id },
                IsActive = false // attempt to deactivate last active admin
            }, adminUser.Id));

            Assert.Equal("Action denied: Cannot deactivate the last remaining Administrator account.", ex.Message);
        }

        [Fact]
        public async Task UserService_AssignRolesToUser_HandlesNullRoleIdsGracefully()
        {
            using var context = CreateInMemoryDbContext();
            await DatabaseSeeder.SeedAsync(context);

            var managerRole = await context.Roles.FirstAsync(r => r.Name == "Manager");
            var userService = new UserService(context);

            var created = await userService.CreateUserAsync(new UserCreateDTO
            {
                Username = "test_null_roles",
                Email = "nullroles@example.com",
                Password = "Password123!",
                RoleId = managerRole.Id
            });

            // Pass null for RoleIds
            var assigned = await userService.AssignRolesToUserAsync(new AssignUserRolesDto
            {
                UserId = created.Id,
                RoleIds = null!
            });

            Assert.True(assigned);
            var updated = await userService.GetUserByIdAsync(created.Id);
            Assert.NotNull(updated);
            Assert.Empty(updated.Roles);
        }

        [Fact]
        public async Task AuthService_Login_ReturnsNullOnEmptyOrNullCredentials()
        {
            using var context = CreateInMemoryDbContext();
            var config = CreateTestConfiguration();
            await DatabaseSeeder.SeedAsync(context);

            var authService = new AuthService(context, config);

            // Null password should return null (401), not throw ArgumentNullException
            var resultNullPass = await authService.LoginAsync(new LoginRequestDto
            {
                Username = "admin",
                Password = null!
            });
            Assert.Null(resultNullPass);

            // Empty username
            var resultEmptyUser = await authService.LoginAsync(new LoginRequestDto
            {
                Username = "",
                Password = "any"
            });
            Assert.Null(resultEmptyUser);
        }
    }
}
