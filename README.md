# UserService.gRPC

Servicio gRPC para gestión de usuarios en .NET 8.

## Descripción
Este proyecto implementa un microservicio gRPC para la administración de usuarios, utilizando Entity Framework Core, FluentValidation, Serilog y autenticación con BCrypt. El servicio está preparado para ejecutarse en contenedores Docker y orquestarse con docker-compose junto a otros servicios como SQL Server.

## Servicios gRPC disponibles
- **GetUser**: Obtiene un usuario por ID.
  - Request: `GetUserRequest { id }`
  - Response: `UserResponse`
- **GetUsers**: Obtiene la lista de todos los usuarios.
  - Request: `GetUsersRequest {}`
  - Response: `GetUsersResponse { users[] }`
- **CreateUser**: Crea un nuevo usuario.
  - Request: `CreateUserRequest { Email, FirstName, LastName, Password, Role }`
  - Response: `UserResponse`
- **UpdateUser**: Actualiza los datos de un usuario existente.
  - Request: `UpdateUserRequest { id, Email, FirstName, LastName, Role, is_active }`
  - Response: `UserResponse`
- **DeleteUser**: Realiza la eliminación lógica (desactivación) de un usuario por ID.
  - Request: `DeleteUserRequest { id }`
  - Response: `DeleteUserResponse { success }`

## Requisitos
- .NET 8 SDK
- Docker y Docker Compose

## Ejecución local
1. Restaurar paquetes NuGet:
   ```bash
   dotnet restore
   ```
2. Ejecutar migraciones y levantar el servicio:
   ```bash
   dotnet ef database update
   dotnet run
   ```

## Ejecución con Docker Compose "Recomendada"
1. Construir y levantar los servicios:
   ```bash   
   docker-compose up --build -d
   ```
2. En Docker Desktop, verificar que los contenedores estén corriendo

3. El servicio gRPC estará disponible en el puerto `7001`.

## Invocación gRPC con Postman
Puedes invocar el servicio gRPC usando Postman desde el siguiente enlace:
[Workspace Postman gRPC](https://web.postman.co/workspace/Noverti~6477bd44-190a-4c56-a720-ed1d91a1ea65/collection/6912f48b7d7b25fed5181afc?action=share&source=copy-link&creator=27540494)

## Detener el servicio de Docker Compose
   ```bash   
    docker-compose stop
    docker-compose down
   ```

## Conectarse a la BD en Local
Server: localhost
Puerto: 1434
Usuario y Pass: se encuentran en `docker-compose.yml`.


## Configuración
- La cadena de conexión a SQL Server se configuran en `docker-compose.yml` y `appsettings.json`.

## Estructura principal
- `Protos/`: Definición de contratos gRPC.
- `Models/`: Modelos de datos.
- `Services/`: Implementación de servicios gRPC.
- `Validators/`: Validaciones con FluentValidation.
- `Program.cs`: Configuración principal de la aplicación.

## Dependencias principales
- Grpc.AspNetCore
- EntityFrameworkCore.SqlServer
- FluentValidation
- Serilog
- BCrypt.Net-Next



## Notas
- Para desarrollo local, asegúrate de que SQL Server estén disponibles.
- Dentro de Docker, los servicios se comunican usando los nombres definidos en `docker-compose.yml`.

## Licencia
Este proyecto es solo para fines educativos.
