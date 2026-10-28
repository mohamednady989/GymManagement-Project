using GymManagementDAL.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace GymManagementDAL.Data.DataSeed
{
    public static class IdentityDataSeeding
    {
        private const string DefaultPassword = "P@ssw0rd";

        public static async Task SeedAsync(
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager,
            ILogger logger,
            CancellationToken ct = default)
        {
            try
            {
                // ==========================================
                // 1. Create Roles
                // ==========================================

                await CreateRoleIfNotExistsAsync(
                    roleManager,
                    "SuperAdmin",
                    logger);

                await CreateRoleIfNotExistsAsync(
                    roleManager,
                    "Admin",
                    logger);


                // ==========================================
                // 2. Create SuperAdmin - Mohamed
                // ==========================================

                await CreateOrUpdateUserAsync(
                    userManager,
                    logger,
                    firstName: "Mohamed",
                    lastName: "Nady",
                    userName: "MohamedNady",
                    email: "mohamedNady@gmail.com",
                    phoneNumber: "01030056881",
                    password: DefaultPassword,
                    role: "SuperAdmin",
                    ct);


                // ==========================================
                // 3. Create SuperAdmin - Aliaa
                // ==========================================

                await CreateOrUpdateUserAsync(
                    userManager,
                    logger,
                    firstName: "Aliaa",
                    lastName: "Tarek",
                    userName: "AliaaTarek",
                    email: "Aliaatarek@gmail.com",
                    phoneNumber: "01123652635",
                    password: DefaultPassword,
                    role: "SuperAdmin",
                    ct);


                // ==========================================
                // 4. Create Admin - Omar
                // ==========================================

                await CreateOrUpdateUserAsync(
                    userManager,
                    logger,
                    firstName: "Omar",
                    lastName: "Mohamed",
                    userName: "OmarMohamed",
                    email: "OmarMohamed@gmail.com",
                    phoneNumber: "01232589652",
                    password: DefaultPassword,
                    role: "Admin",
                    ct);


                logger.LogInformation(
                    "Identity seeding completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Identity seeding failed.");

                throw;
            }
        }


        // ==========================================
        // Create Role
        // ==========================================

        private static async Task CreateRoleIfNotExistsAsync(
            RoleManager<IdentityRole> roleManager,
            string roleName,
            ILogger logger)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                logger.LogInformation(
                    "Role {Role} already exists.",
                    roleName);

                return;
            }

            var result = await roleManager.CreateAsync(
                new IdentityRole(roleName));

            if (!result.Succeeded)
            {
                logger.LogError(
                    "Failed to create role {Role}: {Errors}",
                    roleName,
                    string.Join(
                        "; ",
                        result.Errors.Select(e => e.Description)));

                return;
            }

            logger.LogInformation(
                "Role {Role} created successfully.",
                roleName);
        }


        // ==========================================
        // Create or Update User
        // ==========================================

        private static async Task CreateOrUpdateUserAsync(
            UserManager<ApplicationUser> userManager,
            ILogger logger,
            string firstName,
            string lastName,
            string userName,
            string email,
            string phoneNumber,
            string password,
            string role,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            // ------------------------------------------
            // Find user by Email
            // ------------------------------------------

            var user = await userManager.FindByEmailAsync(email);

            // ------------------------------------------
            // If user doesn't exist, create him
            // ------------------------------------------

            if (user == null)
            {
                user = new ApplicationUser
                {
                    FirstName = firstName,
                    LastName = lastName,
                    UserName = userName,
                    Email = email,
                    PhoneNumber = phoneNumber,

                    // Important
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = false,
                    TwoFactorEnabled = false,
                    LockoutEnabled = true
                };

                var createResult =
                    await userManager.CreateAsync(
                        user,
                        password);

                if (!createResult.Succeeded)
                {
                    logger.LogError(
                        "Failed to create user {Email}: {Errors}",
                        email,
                        string.Join(
                            "; ",
                            createResult.Errors.Select(
                                e => e.Description)));

                    return;
                }

                logger.LogInformation(
                    "User {Email} created successfully.",
                    email);
            }
            else
            {
                // ------------------------------------------
                // User already exists
                // ------------------------------------------

                logger.LogInformation(
                    "User {Email} already exists. Updating data.",
                    email);

                bool updateRequired = false;

                if (user.FirstName != firstName)
                {
                    user.FirstName = firstName;
                    updateRequired = true;
                }

                if (user.LastName != lastName)
                {
                    user.LastName = lastName;
                    updateRequired = true;
                }

                if (user.UserName != userName)
                {
                    user.UserName = userName;
                    updateRequired = true;
                }

                if (user.Email != email)
                {
                    user.Email = email;
                    updateRequired = true;
                }

                if (user.PhoneNumber != phoneNumber)
                {
                    user.PhoneNumber = phoneNumber;
                    updateRequired = true;
                }

                // Important for login
                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    updateRequired = true;
                }

                if (user.LockoutEnabled == false)
                {
                    user.LockoutEnabled = true;
                    updateRequired = true;
                }

                // Remove lockout if there is one
                if (user.LockoutEnd != null)
                {
                    user.LockoutEnd = null;
                    updateRequired = true;
                }

                // Reset failed login attempts
                if (user.AccessFailedCount != 0)
                {
                    user.AccessFailedCount = 0;
                    updateRequired = true;
                }

                if (updateRequired)
                {
                    var updateResult =
                        await userManager.UpdateAsync(user);

                    if (!updateResult.Succeeded)
                    {
                        logger.LogError(
                            "Failed to update user {Email}: {Errors}",
                            email,
                            string.Join(
                                "; ",
                                updateResult.Errors.Select(
                                    e => e.Description)));

                        return;
                    }

                    logger.LogInformation(
                        "User {Email} updated successfully.",
                        email);
                }
            }


            // ==========================================
            // Make sure password is correct
            // ==========================================

            var hasPassword =
                await userManager.HasPasswordAsync(user);

            if (!hasPassword)
            {
                var passwordResult =
                    await userManager.AddPasswordAsync(
                        user,
                        password);

                if (!passwordResult.Succeeded)
                {
                    logger.LogError(
                        "Failed to add password for {Email}: {Errors}",
                        email,
                        string.Join(
                            "; ",
                            passwordResult.Errors.Select(
                                e => e.Description)));

                    return;
                }

                logger.LogInformation(
                    "Password added for {Email}.",
                    email);
            }


            // ==========================================
            // Make sure user has correct role
            // ==========================================

            if (!await userManager.IsInRoleAsync(user, role))
            {
                var roleResult =
                    await userManager.AddToRoleAsync(
                        user,
                        role);

                if (!roleResult.Succeeded)
                {
                    logger.LogError(
                        "Failed to assign role {Role} to {Email}: {Errors}",
                        role,
                        email,
                        string.Join(
                            "; ",
                            roleResult.Errors.Select(
                                e => e.Description)));

                    return;
                }

                logger.LogInformation(
                    "Role {Role} assigned to {Email}.",
                    role,
                    email);
            }
        }
    }
}