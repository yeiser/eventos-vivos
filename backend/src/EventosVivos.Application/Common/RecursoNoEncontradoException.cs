namespace EventosVivos.Application.Common;

/// <summary>Se solicitó un recurso que no existe (se mapea a HTTP 404 en la capa de API).</summary>
public sealed class RecursoNoEncontradoException : Exception
{
    public string Recurso { get; }

    public RecursoNoEncontradoException(string recurso, object id)
        : base($"No se encontró {recurso} con identificador '{id}'.")
    {
        Recurso = recurso;
    }
}
