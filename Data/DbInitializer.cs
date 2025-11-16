using Microsoft.EntityFrameworkCore;
using SunShop.Grpc.Users.Models;
using BC = BCrypt.Net.BCrypt;

namespace SunShop.Grpc.Users.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(UserDbContext context, ILogger logger)
    {
        try
        {
            // Asegurar que la base de datos esté creada
            logger.LogInformation("Verificando existencia de base de datos...");
            await context.Database.EnsureCreatedAsync();

            // Aplicar migraciones pendientes
            if (context.Database.GetPendingMigrations().Any())
            {
                logger.LogInformation("Aplicando migraciones pendientes...");
                await context.Database.MigrateAsync();
            }

            // Verificar si ya existen usuarios
            if (await context.Users.AnyAsync())
            {
                logger.LogInformation("Base de datos ya contiene usuarios. Omitiendo inicialización.");
                return;
            }

            logger.LogInformation("Inicializando base de datos con datos de prueba...");


            var UsersForInicializer = new List<User>
            {
                new() {
                    Email = "juan.perez@empresa.es",
                    FirstName = "Juan",
                    LastName = "Pérez",
                    PasswordHash = BC.HashPassword("Password123!"),
                    Role = "Premium"
                },
                new() {
                    Email = "marie.dubois@societe.fr",
                    FirstName = "Marie",
                    LastName = "Dubois",
                    PasswordHash = BC.HashPassword("Password123!"),
                    Role = "Customer"
                },
                new() {
                    Email = "john.doe@company.com",
                    FirstName = "John",
                    LastName = "Doe",
                    PasswordHash = BC.HashPassword("Password123!"),
                    Role = "Premium"
                },
                new() {
                    Email = "admin@tienda.mx",
                    FirstName = "Admin",
                    LastName = "System",
                    PasswordHash = BC.HashPassword("Admin123!"),
                    Role = "Admin"
                }
            };

            // Agregar usuarios a la base de datos
            await context.Users.AddRangeAsync(UsersForInicializer);
            await context.SaveChangesAsync();

            logger.LogInformation($"Base de datos inicializada exitosamente con {UsersForInicializer.Count} usuarios.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al inicializar la base de datos.");
            throw;
        }
    }
}
