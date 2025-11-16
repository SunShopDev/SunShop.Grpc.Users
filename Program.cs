
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SunShop.Grpc.Users.Data;
using SunShop.Grpc.Users.Protos;
using SunShop.Grpc.Users.Services;
using SunShop.Grpc.Users.Validators;

try
{
    Log.Information("Iniciando UserService gRPC...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Services.AddDbContext<UsersDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });
    });

    // Registrar validadores de FluentValidation
    builder.Services.AddScoped<IValidator<GetUsersRequest>, GetUsersRequestValidator>();
    builder.Services.AddScoped<IValidator<GetUserRequest>, GetUserRequestValidator>();
    builder.Services.AddScoped<IValidator<CreateUserRequest>, CreateUserRequestValidator>();
    builder.Services.AddScoped<IValidator<UpdateUserRequest>, UpdateUserRequestValidator>();
    builder.Services.AddScoped<IValidator<DeleteUserRequest>, DeleteUserRequestValidator>();

    // Registrar servicios gRPC
    builder.Services.AddGrpc(options =>
    {
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();
        options.MaxReceiveMessageSize = 4 * 1024 * 1024; // 4 MB
        options.MaxSendMessageSize = 4 * 1024 * 1024;    // 4 MB
    });

    // Configurar reflexión de gRPC para herramientas de desarrollo
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddGrpcReflection();
    }

    var app = builder.Build();

    // Inicializar base de datos y datos de prueba
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<UsersDbContext>();
            var logger = services.GetRequiredService<ILogger<Program>>();
            await DbInitializer.InitializeAsync(context, logger);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al inicializar la base de datos");
            throw;
        }
    }

    // Configurar pipeline HTTP
    if (app.Environment.IsDevelopment())
    {
        app.MapGrpcReflectionService();
    }

    // Mapear servicio gRPC
    app.MapGrpcService<UsersGrpcService>();

    // Endpoint de salud básico
    app.MapGet("/health", () => Results.Ok(new
    {
        status = "Healthy",
        service = "SunShop.Grpc.Users",
        timestamp = DateTime.UtcNow
    }));

    // Endpoint de información del servicio
    app.MapGet("/", () => Results.Ok(new
    {
        service = "UserService gRPC",
        version = "1.0.0",
        description = "Servicio de gestión de usuarios con comunicación gRPC",
        endpoints = new[]
        {
            "GetUser - Obtiene un usuario por ID",
            "GetUsers - Lista Usuarios con paginación (streaming)",
            "CreateUser - Crea un nuevo Usuario",
            "UpdateUser - Actualiza un Usuario existente",
            "DeleteUser - Elimina un Usuario (lógico)",

        },
        grpcPort = 7001,
        healthCheck = "/health"
    }));

    Log.Information("UserService gRPC iniciado exitosamente en puerto 7001");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La aplicación falló al iniciar");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

