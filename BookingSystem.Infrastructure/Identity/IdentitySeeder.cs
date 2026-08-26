using Microsoft.AspNetCore.Identity;
using BookingSystem.Application.Common;

namespace BookingSystem.Infrastructure.Identity;

// Eseguito all'avvio dell'applicazione (chiamato da Program.cs): crea i ruoli Admin/User
// se non esistono ancora e, se configurato, un utente Admin iniziale — così il sistema
// ha sempre almeno un account con privilegi elevati senza doverlo creare a mano.
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

        // Se email/password non sono configurate (es. ambiente senza AdminSeed nei
        // secrets), si salta la creazione: meglio nessun admin che uno con credenziali vuote.
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
