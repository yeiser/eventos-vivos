namespace EventosVivos.Domain.Eventos;

/// <summary>Estado de un evento (RF-02 filtro, RN06 auto-completado).</summary>
public enum EstadoEvento
{
    Activo = 1,
    Cancelado = 2,
    Completado = 3
}
