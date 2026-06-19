using System.Text.RegularExpressions;
using EventosVivos.Application.Abstractions;
using EventosVivos.Application.Reservas;
using EventosVivos.Application.Reservas.Dtos;
using EventosVivos.Domain.Reservas;
using EventosVivos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventosVivos.Infrastructure.Repositories;

public sealed partial class ReservaRepository : IReservaRepository
{
    private readonly EventosDbContext _db;

    public ReservaRepository(EventosDbContext db) => _db = db;

    public Task<Reserva?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Reservas.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<ReservaResumenDto> Items, int Total)> BuscarAsync(
        ReservaFiltro filtro, CancellationToken cancellationToken = default)
    {
        var query = from r in _db.Reservas.AsNoTracking()
                    join e in _db.Eventos.AsNoTracking() on r.EventoId equals e.Id
                    select new { Reserva = r, EventoTitulo = e.Titulo };

        if (!string.IsNullOrWhiteSpace(filtro.Codigo))
        {
            // El código es exacto: se normaliza la entrada (admite "EV-123456" o "123456").
            var codigo = NormalizarCodigo(filtro.Codigo);
            if (codigo is null)
            {
                return (Array.Empty<ReservaResumenDto>(), 0); // formato inválido → sin resultados
            }
            query = query.Where(x => x.Reserva.Codigo == codigo);
        }

        if (!string.IsNullOrWhiteSpace(filtro.NombreComprador))
            query = query.Where(x => EF.Functions.ILike(x.Reserva.NombreComprador, $"%{filtro.NombreComprador}%"));

        if (filtro.Estado is not null)
            query = query.Where(x => x.Reserva.Estado == filtro.Estado);

        var total = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(x => x.Reserva.FechaReserva)
            .Skip((filtro.PaginaNormalizada - 1) * filtro.TamanoNormalizado)
            .Take(filtro.TamanoNormalizado)
            .ToListAsync(cancellationToken);

        var items = rows.Select(x => new ReservaResumenDto(
            x.Reserva.Id,
            x.Reserva.EventoId,
            x.EventoTitulo,
            x.Reserva.Cantidad,
            x.Reserva.NombreComprador,
            x.Reserva.EmailComprador.Valor,
            x.Reserva.Estado,
            x.Reserva.Codigo?.Valor,
            x.Reserva.FechaReserva)).ToList();

        return (items, total);
    }

    private static CodigoReserva? NormalizarCodigo(string entrada)
    {
        var raw = entrada.Trim().ToUpperInvariant();
        if (SoloDigitos().IsMatch(raw))
            raw = $"EV-{raw}";
        try
        {
            return CodigoReserva.Crear(raw);
        }
        catch
        {
            return null;
        }
    }

    [GeneratedRegex(@"^\d{6}$")]
    private static partial Regex SoloDigitos();
}
