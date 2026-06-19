/** Resultado paginado genérico (espejo de PagedResult<T> del backend). */
export interface PagedResult<T> {
  items: T[];
  pagina: number;
  tamanoPagina: number;
  total: number;
  totalPaginas: number;
}

/** Bloque de trazabilidad incluido en las respuestas de detalle. */
export interface Auditoria {
  creadoPor: string | null;
  fechaCreacion: string;
  modificadoPor: string | null;
  fechaUltimaModificacion: string | null;
}

/** Error RFC 7807 (application/problem+json) devuelto por la API. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  traceId?: string;
  regla?: string;
  errors?: Record<string, string[]>;
  [key: string]: unknown;
}
