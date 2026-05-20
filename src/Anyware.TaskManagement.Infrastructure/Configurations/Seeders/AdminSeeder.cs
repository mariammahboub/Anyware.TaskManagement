using Anyware.TaskManagement.Domain.Entities;
using Anyware.TaskManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Anyware.TaskManagement.Infrastructure.Persistence;
namespace Anyware.TaskManagement.Infrastructure.Configurations.Seeders
{
    public static class AdminSeeder
    {

        public static async Task SeedAsync(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger logger)
        {
            var adminEmail = configuration["AdminSeed:Email"]
                ?? throw new InvalidOperationException(
                    "AdminSeed:Email is not configured in appsettings.json.");

            var adminPassword = configuration["AdminSeed:Password"]
                ?? throw new InvalidOperationException(
                    "AdminSeed:Password is not configured in appsettings.json.");

            var adminName = configuration["AdminSeed:Name"] ?? "Anyware Admin";


            var existingAdmin = await context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == adminEmail.ToLowerInvariant());

            if (existingAdmin is not null)
            {
                logger.LogInformation(
                    "Admin seeder: admin account '{Email}' already exists — skipping.",
                    adminEmail);
                return;
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(
                adminPassword,
                workFactor: 12);

            var admin = User.Create(
                name: adminName,
                email: adminEmail,
                passwordHash: passwordHash,
                role: UserRole.Admin);

            context.Users.Add(admin);
            await context.SaveChangesAsync();

            logger.LogInformation(
                "Admin seeder: created admin account '{Email}' successfully.",
                adminEmail);
        }
    }
}