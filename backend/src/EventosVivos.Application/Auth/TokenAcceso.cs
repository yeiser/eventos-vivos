namespace EventosVivos.Application.Auth;

/// <summary>Token de acceso emitido tras un login correcto.</summary>
public sealed record TokenAcceso(string Token, DateTimeOffset ExpiraEn);
