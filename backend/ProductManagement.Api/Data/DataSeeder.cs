using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using ProductManagement.Api.Configuration;
using ProductManagement.Api.Domain.Entities;

namespace ProductManagement.Api.Data;

public static class DataSeeder
{
    private static readonly string[] BrandNames =
    [
        "Adidas",
        "Nike",
        "New Balance",
        "On Cloud",
        "Puma",
        "Reebok",
        "Asics",
        "Converse",
        "Vans"
    ];

    public static async Task SeedAsync(AppDbContext context, SeedAdminOptions seedAdminOptions)
    {
        var userName = seedAdminOptions.UserName?.Trim();
        var email = seedAdminOptions.Email?.Trim();
        var fullName = seedAdminOptions.FullName?.Trim();
        var password = seedAdminOptions.Password;

        if (!await context.Roles.AnyAsync())
        {
            context.Roles.AddRange(
                new Role { Name = "Admin" },
                new Role { Name = "User" });
        }

        await context.SaveChangesAsync();

        if (!await context.Brands.AnyAsync())
        {
            context.Brands.AddRange(BrandNames.Select(name => new Brand { Name = name }));
            await context.SaveChangesAsync();
        }

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (!await context.Users.AnyAsync(x => x.UserName == userName))
        {
            var adminRoleId = await context.Roles.Where(x => x.Name == "Admin").Select(x => x.Id).SingleAsync();
            context.Users.Add(new User
            {
                UserName = userName,
                Email = email,
                FullName = fullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                RoleId = adminRoleId
            });

            await context.SaveChangesAsync();
        }
    }
}
