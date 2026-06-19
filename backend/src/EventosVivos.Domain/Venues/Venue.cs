using EventosVivos.Domain.Common;

namespace EventosVivos.Domain.Venues;

/// <summary>
/// Lugar físico donde se realizan eventos. Datos de referencia preexistentes (seed).
/// Su <see cref="Capacidad"/> acota la capacidad máxima de los eventos (RN01).
/// </summary>
public sealed class Venue
{
    public int Id { get; private set; }
    public string Nombre { get; private set; } = null!;
    public int Capacidad { get; private set; }
    public string Ciudad { get; private set; } = null!;

    private Venue() { } // EF

    public Venue(int id, string nombre, int capacidad, string ciudad)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DatosInvalidosException("El nombre del venue es obligatorio.");
        if (capacidad <= 0)
            throw new DatosInvalidosException("La capacidad del venue debe ser un entero positivo.");

        Id = id;
        Nombre = nombre.Trim();
        Capacidad = capacidad;
        Ciudad = (ciudad ?? string.Empty).Trim();
    }
}
