using Microsoft.AspNetCore.Identity;
using SIRGA.Infrastructure.Persistence;
using SIRGA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace SIRGA.Infrastructure;

public static class DbInitializer
{
  public static async Task SeedIdentityAsync(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
  {
    // ... roles ...
    // ... usuarios ...
  }

  public static async Task SeedLocationsAsync(SIRG_DbContext context)
  {
    if (await context.Provinces.AnyAsync()) return;

    var provinces = new List<Province>
    {
        new Province { Name = "Buenos Aires", Cities = new List<City>
            {
                new City { Name = "La Plata", ZipCode = "1900" },
                new City { Name = "Mar del Plata", ZipCode = "7600" }
            }
        },
        new Province { Name = "Córdoba", Cities = new List<City>
            {
                new City { Name = "Córdoba Capital", ZipCode = "5000" },
                new City { Name = "Villa Carlos Paz", ZipCode = "5152" }
            }
        },
        new Province { Name = "Santa Fe", Cities = new List<City>
            {
                new City { Name = "Rosario", ZipCode = "2000" },
                new City { Name = "Santa Fe", ZipCode = "3000" }
            }
        }
    };

    await context.Provinces.AddRangeAsync(provinces);
    await context.SaveChangesAsync();
  }

  private static async Task CreateUserIfNotExists(UserManager<IdentityUser> userManager, string email, string password, string role)
  {
    if (await userManager.FindByEmailAsync(email) == null)
    {
      var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
      var result = await userManager.CreateAsync(user, password);
      if (result.Succeeded)
      {
        await userManager.AddToRoleAsync(user, role);
      }
    }
  }
}
