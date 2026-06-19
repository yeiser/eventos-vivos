import { Auditoria } from './common.models';

export type EstadoReserva = 'pendiente_pago' | 'confirmada' | 'cancelada' | 'perdida';

export const ESTADOS_RESERVA: EstadoReserva[] = ['pendiente_pago', 'confirmada', 'cancelada', 'perdida'];

/** Resultado de la búsqueda global de reservas (incluye el título del evento). */
export interface ReservaResumen {
  id: string;
  eventoId: string;
  eventoTitulo: string;
  cantidad: number;
  nombreComprador: string;
  emailComprador: string;
  estado: EstadoReserva;
  codigo: string | null;
  fechaReserva: string;
}

export interface ReservaFiltro {
  codigo?: string;
  nombreComprador?: string;
  estado?: EstadoReserva;
  pagina?: number;
  tamanoPagina?: number;
}

export interface Reserva {
  id: string;
  eventoId: string;
  cantidad: number;
  nombreComprador: string;
  emailComprador: string;
  estado: EstadoReserva;
  codigo: string | null;
  fechaReserva: string;
  fechaConfirmacion: string | null;
  fechaCancelacion: string | null;
  auditoria: Auditoria;
}

export interface CrearReservaRequest {
  cantidad: number;
  nombreComprador: string;
  emailComprador: string;
}
