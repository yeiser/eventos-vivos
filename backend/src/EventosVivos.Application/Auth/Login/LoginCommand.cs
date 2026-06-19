namespace EventosVivos.Application.Auth.Login;

public sealed record LoginCommand(string NombreUsuario, string Password);

/// <summary>Respuesta de un login correcto.</summary>
public sealed record LoginResponse(string Token, DateTimeOffset ExpiraEn, string NombreUsuario, string Rol);
