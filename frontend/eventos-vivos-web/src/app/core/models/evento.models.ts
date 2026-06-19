import { Auditoria } from './common.models';

export type TipoEvento = 'conferencia' | 'taller' | 'concierto';
export type EstadoEvento = 'activo' | 'cancelado' | 'completado';

export const TIPOS_EVENTO: TipoEvento[] = ['conferencia', 'taller', 'concierto'];
export const ESTADOS_EVENTO: EstadoEvento[] = ['activo', 'cancelado', 'completado'];

export interface Evento {
  id: string;
  titulo: string;
  descripcion: string;
  venueId: number;
  capacidadMaxima: number;
  fechaInicio: string;
  fechaFin: string;
  precio: number;
  tipo: TipoEvento;
  estado: EstadoEvento;
  entradasVendidas: number;
  entradasDisponibles: number;
  auditoria: Auditoria;
}

export interface EventoResumen {
  id: string;
  titulo: string;
  venueId: number;
  fechaInicio: string;
  fechaFin: string;
  precio: number;
  tipo: TipoEvento;
  estado: EstadoEvento;
  capacidadMaxima: number;
  entradasDisponibles: number;
}

export interface ReporteOcupacion {
  eventoId: string;
  titulo: string;
  estado: EstadoEvento;
  capacidadMaxima: number;
  entradasVendidas: number;
  entradasDisponibles: number;
  porcentajeOcupacion: number;
  ingresosTotales: number;
}

export interface CrearEventoRequest {
  titulo: string;
  descripcion: string;
  venueId: number;
  capacidadMaxima: number;
  fechaInicio: string;
  fechaFin: string;
  precio: number;
  tipo: TipoEvento;
}

export interface EventoFiltro {
  tipo?: TipoEvento;
  venueId?: number;
  estado?: EstadoEvento;
  fechaInicioDesde?: string;
  fechaInicioHasta?: string;
  titulo?: string;
  pagina?: number;
  tamanoPagina?: number;
}
