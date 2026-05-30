using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using StudyCourseAPI.Configurations;
using StudyCourseAPI.Models;

namespace StudyCourseAPI.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAdminAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<Role> roleManager,
            IConfiguration config)
        {
            // 1. Ensure roles
            if (!await roleManager.RoleExistsAsync(AppRoles.Admin))
                await roleManager.CreateAsync(new Role(AppRoles.Admin));

            if (!await roleManager.RoleExistsAsync(AppRoles.User))
                await roleManager.CreateAsync(new Role(AppRoles.User));

            // 2. Get admin config
            var adminConfig = config.GetSection("AdminAccount").Get<AdminAccountConfig>();

            // 3. Check admin exists
            var existingAdmin = await userManager.FindByNameAsync(adminConfig.Username);
            if (existingAdmin != null)
            {
                // ✅ Assign role nếu chưa có
                var roles = await userManager.GetRolesAsync(existingAdmin);
                if (!roles.Contains(AppRoles.Admin))
                    await userManager.AddToRoleAsync(existingAdmin, AppRoles.Admin);
    
                return;
            }

            // 4. Create admin
            var admin = new ApplicationUser
            {
                UserName = adminConfig.Username,
                Email = adminConfig.Email,
                EmailConfirmed = true,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(admin, adminConfig.Password);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, AppRoles.Admin);
            }
        }

        public static async Task SeedUserAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<Role> roleManager,
            IConfiguration config)
        {
            // 1. Ensure User role exists
            if (!await roleManager.RoleExistsAsync(AppRoles.User))
                await roleManager.CreateAsync(new Role(AppRoles.User));

            // 2. Get user config
            var userConfig = config.GetSection("UserAccount").Get<AdminAccountConfig>();

            // 3. Check user exists
            var existingUser = await userManager.FindByNameAsync(userConfig.Username);
            if (existingUser != null)
            {
                // ✅ Assign role nếu chưa có
                var roles = await userManager.GetRolesAsync(existingUser);
                if (!roles.Contains(AppRoles.User))
                    await userManager.AddToRoleAsync(existingUser, AppRoles.User);
    
                return;
            }

            // 4. Create user
            var user = new ApplicationUser
            {
                UserName = userConfig.Username,
                Email = userConfig.Email,
                EmailConfirmed = true,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, userConfig.Password);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, AppRoles.User);
            }
        }
    }
}