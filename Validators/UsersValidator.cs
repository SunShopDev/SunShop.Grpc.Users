using FluentValidation;
using SunShop.Grpc.Users.Protos;

namespace SunShop.Grpc.Users.Validators;

public class GetUsersRequestValidator : AbstractValidator<GetUsersRequest>
{
    public GetUsersRequestValidator()
    {
        RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("El número de página debe ser mayor a cero");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("El tamaño de página debe ser mayor a cero")
            .LessThanOrEqualTo(100).WithMessage("El tamaño de página no puede exceder 100 elementos");
    }
}
public class GetUserRequestValidator : AbstractValidator<GetUserRequest>
{
    public GetUserRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("El ID del usuario debe ser mayor a cero");
    }
}
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().EmailAddress().WithMessage("Email válido es requerido");
        RuleFor(x => x.Password)
            .NotEmpty().MinimumLength(6).WithMessage("Password mínimo 6 caracteres");
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es requerido");
        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("El apellido es requerido");
    }
}
public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("El ID del usuario debe ser mayor a cero");
        RuleFor(x => x.Email)
            .NotEmpty().EmailAddress().WithMessage("Email válido es requerido");
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es requerido");
        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("El apellido es requerido");
    }
}
public class DeleteUserRequestValidator : AbstractValidator<DeleteUserRequest>
{
    public DeleteUserRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("El ID del usuario debe ser mayor a cero");
    }
}