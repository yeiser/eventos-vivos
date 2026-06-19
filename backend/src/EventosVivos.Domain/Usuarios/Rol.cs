namespace EventosVivos.Domain.Usuarios;

/// <summary>Roles de autorización (ADR-007). Admin confirma pago/crea eventos; Usuario reserva/cancela.</summary>
public enum Rol
{
    Usuario = 1,
    Admin = 2
}
