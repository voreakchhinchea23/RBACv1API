using Microsoft.EntityFrameworkCore;
using RbacApi.Data;
using RbacApi.DTOs;
using RbacApi.Models;
using RbacApi.Services;
using Xunit;

namespace RbacApi.Tests
{
    public class RoleAndPermissionTests
    {
        private AppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task RoleService_CRUD_And_PermissionAssignments()
        {
            using var context = CreateInMemoryDbContext();
            var roleService = new RoleService(context);

            // 1. Create Role
            var createdRole = await roleService.CreateRoleAsync(new RoleCreateDto
            {
                Name = "Auditor",
                Description = "System Auditor"
            });
            Assert.NotNull(createdRole);
            Assert.Equal("Auditor", createdRole.Name);

            // 2. Add permissions to DB
            var perm1 = new Permission { Name = "audit.read", Module = "AUDIT", Description = "Allows audit read" };
            var perm2 = new Permission { Name = "audit.export", Module = "AUDIT", Description = "Allows audit export" };
            context.Permissions.AddRange(perm1, perm2);
            await context.SaveChangesAsync();

            // 3. Assign permissions via list
            var assigned = await roleService.AssignPermissionToRoleAsync(new AssignPermissionsDto
            {
                RoleId = createdRole.Id,
                PermissionIds = new List<int> { perm1.Id, perm2.Id }
            });
            Assert.True(assigned);

            var roleWithPerms = await roleService.GetRoleByIdAsync(createdRole.Id);
            Assert.NotNull(roleWithPerms);
            Assert.Equal(2, roleWithPerms.Permissions.Count);

            // 4. Remove single permission
            var removed = await roleService.RemoveSinglePermissionAsync(createdRole.Id, perm1.Id);
            Assert.True(removed);

            var roleAfterRemove = await roleService.GetRoleByIdAsync(createdRole.Id);
            Assert.NotNull(roleAfterRemove);
            Assert.Single(roleAfterRemove.Permissions);
            Assert.Equal("audit.export", roleAfterRemove.Permissions.First().Name);

            // 5. Add single permission back
            var added = await roleService.AddSinglePermissionAsync(createdRole.Id, perm1.Id);
            Assert.True(added);

            var roleAfterAdd = await roleService.GetRoleByIdAsync(createdRole.Id);
            Assert.NotNull(roleAfterAdd);
            Assert.Equal(2, roleAfterAdd.Permissions.Count);

            // 6. Delete role
            var deleted = await roleService.DeleteRoleAsync(createdRole.Id);
            Assert.True(deleted);

            var checkDeleted = await roleService.GetRoleByIdAsync(createdRole.Id);
            Assert.Null(checkDeleted);
        }

        [Fact]
        public async Task PermissionService_CRUD_Operations()
        {
            using var context = CreateInMemoryDbContext();
            var permService = new PermissionService(context);

            // 1. Create permission
            var created = await permService.CreatePermissionAsync(new PermissionCreateDto
            {
                Name = "report.view",
                Module = "REPORT",
                Description = "View reports"
            });
            Assert.NotNull(created);
            Assert.Equal("report.view", created.Name);

            // 2. Get permission by id
            var fetched = await permService.GetPermissionByIdAsync(created.Id);
            Assert.NotNull(fetched);
            Assert.Equal("report.view", fetched.Name);

            // 3. Update permission
            var updated = await permService.UpdatePermissionAsync(new PermissionUpdateDto
            {
                Name = "report.view_all",
                Module = "REPORT",
                Description = "View all reports"
            }, created.Id);
            Assert.True(updated);

            var fetchedAfterUpdate = await permService.GetPermissionByIdAsync(created.Id);
            Assert.NotNull(fetchedAfterUpdate);
            Assert.Equal("report.view_all", fetchedAfterUpdate.Name);

            // 4. Delete permission
            var deleted = await permService.DeletePermissionAsync(created.Id);
            Assert.True(deleted);

            var fetchedAfterDelete = await permService.GetPermissionByIdAsync(created.Id);
            Assert.Null(fetchedAfterDelete);
        }

        [Fact]
        public async Task RoleService_DeleteRole_PreventsDeletingAdminRole()
        {
            using var context = CreateInMemoryDbContext();
            await DatabaseSeeder.SeedAsync(context);

            var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");
            var roleService = new RoleService(context);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => roleService.DeleteRoleAsync(adminRole.Id));
            Assert.Equal("Action denied: Cannot delete the core Administrator role.", ex.Message);
        }

        [Fact]
        public async Task RoleService_UpdateRole_PreventsRenamingAdminRole()
        {
            using var context = CreateInMemoryDbContext();
            await DatabaseSeeder.SeedAsync(context);

            var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");
            var roleService = new RoleService(context);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => roleService.UpdateRoleAsync(new RoleUpdateDto
            {
                Name = "RenamedAdmin",
                Description = "Trying to rename Admin"
            }, adminRole.Id));

            Assert.Equal("Action denied: Cannot rename the core Administrator role.", ex.Message);
        }

        [Fact]
        public async Task RoleService_AssignPermission_HandlesNullPermissionIdsGracefully()
        {
            using var context = CreateInMemoryDbContext();
            await DatabaseSeeder.SeedAsync(context);

            var managerRole = await context.Roles.FirstAsync(r => r.Name == "Manager");
            var roleService = new RoleService(context);

            // Pass null for PermissionIds
            var assigned = await roleService.AssignPermissionToRoleAsync(new AssignPermissionsDto
            {
                RoleId = managerRole.Id,
                PermissionIds = null!
            });

            Assert.True(assigned);
            var updated = await roleService.GetRoleByIdAsync(managerRole.Id);
            Assert.NotNull(updated);
            Assert.Empty(updated.Permissions);
        }
    }
}
