using Microsoft.AspNetCore.Identity;

namespace BookingSystem.Infrastructure.Identity;

public static class IdentitySeeder
{
    private const string AdminEmail = "admin@bookingsystem.com";
    private const string AdminPassword = "Admin123!";

    public static async Task SeedAsync(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
    {
        foreach (var role in new[] { "Admin", "User" })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var adminUser = await userManager.FindByEmailAsync(AdminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                EmailConfirmed = true
            };

            await userManager.CreateAsync(adminUser, AdminPassword);
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}
