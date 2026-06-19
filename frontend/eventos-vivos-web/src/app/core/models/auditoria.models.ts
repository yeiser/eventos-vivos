export type AccionAuditoria = 'crear' | 'actualizar' | 'eliminar';

export interface AuditLog {
  id: string;
  entidad: string;
  entidadId: string;
  accion: AccionAuditoria;
  usuario: string;
  fecha: string;
  valoresAnteriores: string | null;
  valoresNuevos: string | null;
  camposModificados: string | null;
  traceId: string | null;
  ipOrigen: string | null;
}

export interface AuditoriaFiltro {
  entidad?: string;
  entidadId?: string;
  usuario?: string;
  accion?: AccionAuditoria;
  fechaDesde?: string;
  fechaHasta?: string;
  pagina?: number;
  tamanoPagina?: number;
}
