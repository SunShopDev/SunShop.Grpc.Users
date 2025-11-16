using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using SunShop.Grpc.Users.Data;
using SunShop.Grpc.Users.Models;
using SunShop.Grpc.Users.Protos;

namespace SunShop.Grpc.Users.Services;

public class UserGrpcService : global::SunShop.Grpc.Users.Protos.UserService.UserServiceBase
{
    private readonly UserDbContext _context;
    private readonly ILogger<UserGrpcService> _logger;
    private readonly IValidator<GetUsersRequest> _getUsersValidator;
    private readonly IValidator<GetUserRequest> _getUserValidator;
    private readonly IValidator<CreateUserRequest> _createUserValidator;
    private readonly IValidator<UpdateUserRequest> _updateUserValidator;
    private readonly IValidator<DeleteUserRequest> _deleteUserValidator;

    public UserGrpcService( UserDbContext context, ILogger<UserGrpcService> logger, 
                            IValidator<GetUserRequest> getUserValidator,
                            IValidator<GetUsersRequest> getUsersValidator,
                            IValidator<CreateUserRequest> createUserValidator,
                            IValidator<UpdateUserRequest> updateUserValidator,
                            IValidator<DeleteUserRequest> deleteUserValidator )
    {
        _context = context;
        _logger = logger;
        _getUserValidator = getUserValidator;
        _getUsersValidator = getUsersValidator;
        _createUserValidator = createUserValidator;
        _updateUserValidator = updateUserValidator;
        _deleteUserValidator = deleteUserValidator;
    }

    public override async Task GetUsers(GetUsersRequest request, IServerStreamWriter<UserResponse> responseStream, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("GetUsers llamado con paginación: " + $"Página={request.PageNumber}, Tamaño={request.PageSize}");

            #region Validaciones

            var validationResult = await _getUsersValidator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));

                _logger.LogWarning($"Validación fallida: {errors}");

                throw new RpcException(new Status(StatusCode.InvalidArgument, errors));
            }
            #endregion

            #region Filtros

            var query = _context.Users.AsQueryable();


            if (request.ActiveOnly)
            {
                query = query.Where(p => p.IsActive);
            }
            #endregion

            #region Ordenamiento            
            query = query.OrderBy(p => p.FirstName);
            #endregion

            #region Buscar Usuarios - Con Paginación
            // Calcular paginación
            var pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;
            var pageSize = request.PageSize > 0 ? request.PageSize : 10;
            var skip = (pageNumber - 1) * pageSize;

            // Obtener usuarios con paginación
            var users = await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            _logger.LogInformation($"Se encontraron {users.Count} usuarios");
            #endregion

            #region Devolver Usuarios            
            foreach (var user in users)
            {
                if (context.CancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("GetUsers cancelado por el cliente");
                    break;
                }

                var response = MapToUserResponse(user);

                await responseStream.WriteAsync(response);
            }

            _logger.LogInformation($"GetUsers completado - {users.Count} usuarios enviados");
            #endregion
        }

        #region Manejo de Excepciones
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener lista de usuarios");

            throw new RpcException(new Status(StatusCode.Internal, "Error interno al procesar la solicitud"));
        }
        #endregion
    }

    public override async Task<UserResponse> GetUser(GetUserRequest request, ServerCallContext context)
    {       
        try
        {
            _logger.LogInformation($"GetUser llamado para ID: {request.Id}");

            #region Validaciones
            var validationResult = await _getUserValidator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));

                _logger.LogWarning($"Validación fallida: {errors}");

                throw new RpcException(new Status(StatusCode.InvalidArgument, errors));
            }
            #endregion

            #region Buscar Usuario
            var user = await _context.Users.FirstOrDefaultAsync(p => p.Id == request.Id);

            if (user == null)
            {
                _logger.LogWarning($"User con ID {request.Id} no encontrado");

                throw new RpcException(new Status(StatusCode.NotFound, $"User con ID {request.Id} no existe"));
            }

            _logger.LogInformation($"User {user.Email} encontrado exitosamente");
            #endregion

            #region Devolver Usuario
            return MapToUserResponse(user);
            #endregion
        }

        #region Manejo de Excepciones
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error al obtener usuario con ID {request.Id}");
            
            throw new RpcException(new Status(StatusCode.Internal, "Error interno al procesar la solicitud"));
        } 
        #endregion

    }
    
    public override async Task<UserResponse> CreateUser(CreateUserRequest request, ServerCallContext context)
    {     
        try
        {
            _logger.LogInformation($"CreateUser llamado para: {request.FirstName}");

            #region Validaciones
            var validationResult = await _createUserValidator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));

                _logger.LogWarning($"Validación fallida: {errors}");

                throw new RpcException(new Status(StatusCode.InvalidArgument, errors));
            }

            // Verificar si ya existe un usuario con el mismo nombre
            var existingUserByEmail = await _context.Users.FirstOrDefaultAsync(p => p.Email == request.Email);

            if (existingUserByEmail != null)
            {
                _logger.LogWarning($"Usuario con Email '{request.Email}' ya existe");

                throw new RpcException(new Status(StatusCode.AlreadyExists, $"Ya existe un usuario con el Email '{request.Email}'"));
            }
            #endregion

            #region Crea Usuario
            var user = new User
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password), //TODO: Se debería autogenerar
                Role = string.IsNullOrWhiteSpace(request.Role) ? "Customer" : request.Role,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Usuario creado exitosamente con ID: {user.Id}");
            #endregion

            #region Devolver Usuario Creado
            return MapToUserResponse(user); 
            #endregion
        }

        #region Manejo de Excepciones
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear Usuario");

            throw new RpcException(new Status(StatusCode.Internal, "Error interno al procesar la solicitud"));
        } 
        #endregion
    }

    public override async Task<UserResponse> UpdateUser(UpdateUserRequest request, ServerCallContext context)
    {   try
        {
            _logger.LogInformation($"UpdateUser llamado para ID: {request.Id}");

            #region Validaciones
            var validationResult = await _updateUserValidator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));

                _logger.LogWarning($"Validación fallida: {errors}");

                throw new RpcException(new Status(StatusCode.InvalidArgument, errors));
            }

            var user = await _context.Users.FirstOrDefaultAsync(p => p.Id == request.Id);

            if (user == null)
            {
                _logger.LogWarning($"User con ID {request.Id} no encontrado");

                throw new RpcException(new Status(StatusCode.NotFound, $"Usuario con ID {request.Id} no existe"));
            }

            // Verificar si el nuevo email ya existe en otro usuario
            if (user.Email != request.Email)
            {
                var existingUserByEmail = await _context.Users.FirstOrDefaultAsync(p => p.Email == request.Email && p.Id != request.Id);

                if (existingUserByEmail != null)
                {
                    _logger.LogWarning($"Usuario con Email '{request.FirstName}' ya existe");

                    throw new RpcException(new Status(StatusCode.AlreadyExists, $"Ya existe otro Usuario con el Email '{request.FirstName}'"));
                }
            }
            #endregion

            #region Actualiza Usuario
            user.Email = request.Email;
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Role = request.Role;
            user.IsActive = request.IsActive;

            _context.Users.Update(user);

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Usuario {user.Id} actualizado exitosamente");
            #endregion

            #region Devolver Usuario Actualizado
            return MapToUserResponse(user); 
            #endregion
        }

        #region Manejo de Excepciones
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error al actualizar usuario con ID {request.Id}");

            throw new RpcException(new Status(StatusCode.Internal, "Error interno al procesar la solicitud"));
        } 
        #endregion
    }

    public override async Task<DeleteUserResponse> DeleteUser(DeleteUserRequest request, ServerCallContext context)
    {
        try
        {            
            _logger.LogInformation($"DeleteUser llamado para ID: {request.Id}");
            
            #region Validaciones
            var validationResult = await _deleteUserValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));

                _logger.LogWarning($"Validación fallida: {errors}");
                
                throw new RpcException(new Status(StatusCode.InvalidArgument, errors));
            }
            #endregion

            #region Busca Usuario
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.Id);
            if (user == null)
            {
                _logger.LogWarning($"Usuario con ID {request.Id} no encontrado");

                throw new RpcException(new Status(StatusCode.NotFound, $"Usuario con ID {request.Id} no existe"));
            }
            #endregion

            #region Elimina Usuario Logicamente

            user.IsActive = false;            

            _context.Users.Update(user);

            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Usuario {user.Id} eliminado exitosamente (lógico)");
            #endregion

            #region Devolver Mensaje De Eliminación
            return new DeleteUserResponse
            {
                Success = true,
                Message = $"Usuario con ID {request.Id} eliminado exitosamente"
            }; 
            #endregion
        }

        #region Manejo de Excepciones
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error al eliminar usuario con ID {request.Id}");

            throw new RpcException(new Status(StatusCode.Internal, "Error interno al procesar la solicitud"));
        }
        #endregion
    }

    private UserResponse MapToUserResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName, 
            Role = user.Role,
            CreatedAt = user.CreatedAt.ToString("o"),
            LastLogin = user.LastLogin?.ToString("o") ?? string.Empty,
            IsActive = user.IsActive
        };
    }
}