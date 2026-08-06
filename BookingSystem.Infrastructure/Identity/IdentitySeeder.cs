using Microsoft.AspNetCore.Identity;
using BookingSystem.Application.Common;

namespace BookingSystem.Infrastructure.Identity;

public static class IdentitySeeder
{
    public static async Task SeedAsync(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager, AdminSeedOptions adminSeed)
    {
        foreach (var role in new[] { Roles.Admin, Roles.User })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        if (string.IsNullOrWhiteSpace(adminSeed.Email) || string.IsNullOrWhiteSpace(adminSeed.Password))
        {
            return;
        }

        var adminUser = await userManager.FindByEmailAsync(adminSeed.Email);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminSeed.Email,
                Email = adminSeed.Email,
                EmailConfirmed = true
            };

            await userManager.CreateAsync(adminUser, adminSeed.Password);
            await userManager.AddToRoleAsync(adminUser, Roles.Admin);
        }
    }
}
