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
            // 1. SEED ROLES ("Admin", "Manager", "Visitor")
            // =========================================================
            var rolesToSeed = new List<Role>
            {
                new Role { Name = "Admin", Description = "Super Administrator with full permissions" },
                new Role { Name = "Manager", Description = "Department Manager with elevated access" },
                new Role { Name = "Visitor", Description = "Visitor can only view and visit the app." }
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
            // 3. ASSIGN ALL PERMISSIONS TO "Admin" ROLE & READ TO "Visitor"
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

            var visitorRole = await context.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Name == "Visitor");

            if (visitorRole != null)
            {
                var readPerms = allDbPermissions
                    .Where(p => p.Name == Perm.USER_READ || p.Name == Perm.ROLE_READ || p.Name == Perm.PERMISSION_READ)
                    .ToList();

                foreach (var perm in readPerms)
                {
                    if (!visitorRole.RolePermissions.Any(rp => rp.PermissionId == perm.Id))
                    {
                        visitorRole.RolePermissions.Add(new RolePermission
                        {
                            RoleId = visitorRole.Id,
                            PermissionId = perm.Id
                        });
                    }
                }
            }

            await context.SaveChangesAsync();


            // =========================================================
            // 4. SEED ADMIN USER ("admin", "admin@rbac.local", "Abc123$")
            // =========================================================
            var adminUser = await context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Username == "admin" || u.Email == "admin@rbac.local");

            if (adminUser == null)
            {
                // Hash the password securely using BCrypt
                string passwordHash = BCrypt.Net.BCrypt.HashPassword("Abc123$");

                adminUser = new User
                {
                    Username = "admin",
                    Email = "admin@rbac.local",
                    Password = passwordHash,
                    IsActive = true,
                    Phone = "0123456789"
                };

                context.Users.Add(adminUser);
                await context.SaveChangesAsync();
            }

            // Ensure Admin role is linked via UserRole
            bool hasAdminRole = await context.UserRoles
                .AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRole.Id);

            if (!hasAdminRole)
            {
                context.UserRoles.Add(new UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = adminRole.Id
                });
                await context.SaveChangesAsync();
            }
        }
    }
}