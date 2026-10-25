using Microsoft.EntityFrameworkCore;
using RbacApi.Authorization;
using RbacApi.Models;

namespace RbacApi.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // =========================================================
            // 1. SEED ROLES ("Admin", "Manager")
            // =========================================================
            var rolesToSeed = new List<Role>
            {
                new Role { Name = "Admin", Description = "Super Administrator with full permissions" },
                new Role { Name = "Manager", Description = "Department Manager with elevated access" }
            };

            foreach (var role in rolesToSeed)
            {
                if (!await context.Roles.AnyAsync(r => r.Name == role.Name))
                {
                    context.Roles.Add(role);
                }
            }
            await context.SaveChangesAsync();

            // Fetch the Admin role with its permissions for subsequent steps
            var adminRole = await context.Roles
                .Include(r => r.RolePermissions)
                .FirstAsync(r => r.Name == "Admin");


            // =========================================================
            // 2. SEED ALL PERMISSIONS FROM Perm.GetAll()
            // =========================================================
            var allPermCodes = Perm.GetAll();
            var existingPermNames = await context.Permissions
                .Select(p => p.Name)
                .ToListAsync();

            foreach (var permCode in allPermCodes)
            {
                if (!existingPermNames.Contains(permCode))
                {
                    context.Permissions.Add(new Permission
                    {
                        Name = permCode,
                        Module = permCode.Split('.')[0].ToUpper(), // "USER", "ROLE", "PERMISSION"
                        Description = $"Allows {permCode}"
                    });
                }
            }
            await context.SaveChangesAsync();


            // =========================================================
            // 3. ASSIGN ALL PERMISSIONS TO "Admin" ROLE
            // =========================================================
            var allDbPermissions = await context.Permissions.ToListAsync();

            foreach (var perm in allDbPermissions)
            {
                bool alreadyAssigned = adminRole.RolePermissions
                    .Any(rp => rp.PermissionId == perm.Id);

                if (!alreadyAssigned)
                {
                    adminRole.RolePermissions.Add(new RolePermission
                    {
                        RoleId = adminRole.Id,
                        PermissionId = perm.Id
                    });
                }
            }
            await context.SaveChangesAsync();


            // =========================================================
            // 4. SEED ADMIN USER ("admin", "admin@rbac.local", "Abc123$")
            // =========================================================
            bool adminUserExists = await context.Users
                .AnyAsync(u => u.Username == "admin" || u.Email == "admin@rbac.local");

            if (!adminUserExists)
            {
                // Hash the password securely using BCrypt
                string passwordHash = BCrypt.Net.BCrypt.HashPassword("Abc123$");

                var adminUser = new User
                {
                    Username = "admin",
                    Email = "admin@rbac.local",
                    Password = passwordHash,
                    RoleId = adminRole.Id, // Link directly to Admin role
                    IsActive = true,
                    Phone = "0123456789"
                };

                context.Users.Add(adminUser);
                await context.SaveChangesAsync();
            }
        }
    }
}