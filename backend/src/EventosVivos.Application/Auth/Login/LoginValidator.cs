using FluentValidation;

namespace EventosVivos.Application.Auth.Login;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.NombreUsuario).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}
