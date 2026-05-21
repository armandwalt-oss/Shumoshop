using Microsoft.AspNetCore.Identity;

public class RoleSeeder

{
    public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roleNames = { "Dev", "Admin", "User" };

        foreach (var roleName in roleNames)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }
}