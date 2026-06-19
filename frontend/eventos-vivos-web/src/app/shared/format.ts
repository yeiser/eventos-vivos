import { AccionAuditoria } from '../core/models/auditoria.models';
import { EstadoEvento, TipoEvento } from '../core/models/evento.models';
import { EstadoReserva } from '../core/models/reserva.models';

const TIPO: Record<TipoEvento, string> = {
  conferencia: 'Conferencia',
  taller: 'Taller',
  concierto: 'Concierto',
};

const ESTADO_EVENTO: Record<EstadoEvento, { etiqueta: string; badge: string }> = {
  activo: { etiqueta: 'Activo', badge: 'badge-light-success' },
  cancelado: { etiqueta: 'Cancelado', badge: 'badge-light-danger' },
  completado: { etiqueta: 'Completado', badge: 'badge-light-secondary' },
};

const ESTADO_RESERVA: Record<EstadoReserva, { etiqueta: string; badge: string }> = {
  pendiente_pago: { etiqueta: 'Pendiente de pago', badge: 'badge-light-warning' },
  confirmada: { etiqueta: 'Confirmada', badge: 'badge-light-success' },
  cancelada: { etiqueta: 'Cancelada', badge: 'badge-light-secondary' },
  perdida: { etiqueta: 'Perdida', badge: 'badge-light-danger' },
};

/**
 * Convierte el valor de un input datetime-local ("YYYY-MM-DDTHH:mm") en un ISO 8601 con el offset
 * local del navegador, preservando la hora de pared (relevante para reglas como RN03).
 */
export function aIsoConOffset(local: string): string {
  const d = new Date(local);
  const pad = (n: number) => String(n).padStart(2, '0');
  const offsetMin = -d.getTimezoneOffset();
  const signo = offsetMin >= 0 ? '+' : '-';
  const abs = Math.abs(offsetMin);
  const fecha = `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
  const hora = `${pad(d.getHours())}:${pad(d.getMinutes())}:00`;
  return `${fecha}T${hora}${signo}${pad(Math.floor(abs / 60))}:${pad(abs % 60)}`;
}

/**
 * Límite efectivo de entradas por transacción (espejo de la composición A-02 del backend).
 * Devuelve 0 cuando no se puede reservar (faltan menos de 1 hora — RN04). La verdad de negocio
 * la valida el backend; esto es solo para la UX.
 */
export function limiteEntradas(fechaInicio: string, precio: number, disponibles: number): number {
  const minutos = (new Date(fechaInicio).getTime() - Date.now()) / 60000;
  if (minutos < 60) {
    return 0; // RN04
  }
  let limite = disponibles;
  if (minutos < 24 * 60) {
    limite = Math.min(limite, 5); // RF-03
  }
  if (precio > 100) {
    limite = Math.min(limite, 10); // RN05
  }
  return Math.max(0, limite);
}

export const etiquetaTipo = (t: TipoEvento): string => TIPO[t] ?? t;
export const etiquetaEstadoEvento = (e: EstadoEvento): string => ESTADO_EVENTO[e]?.etiqueta ?? e;
export const badgeEstadoEvento = (e: EstadoEvento): string => ESTADO_EVENTO[e]?.badge ?? 'badge-light';
export const etiquetaEstadoReserva = (e: EstadoReserva): string => ESTADO_RESERVA[e]?.etiqueta ?? e;
export const badgeEstadoReserva = (e: EstadoReserva): string => ESTADO_RESERVA[e]?.badge ?? 'badge-light';

const ACCION: Record<AccionAuditoria, { etiqueta: string; badge: string }> = {
  crear: { etiqueta: 'Creación', badge: 'badge-light-success' },
  actualizar: { etiqueta: 'Actualización', badge: 'badge-light-warning' },
  eliminar: { etiqueta: 'Eliminación', badge: 'badge-light-danger' },
};

export const etiquetaAccion = (a: AccionAuditoria): string => ACCION[a]?.etiqueta ?? a;
export const badgeAccion = (a: AccionAuditoria): string => ACCION[a]?.badge ?? 'badge-light';

/** Formatea un string JSON a una versión legible (indentada); si no es JSON, lo devuelve tal cual. */
export function formatearJson(valor: string | null): string {
  if (!valor) {
    return '';
  }
  try {
    return JSON.stringify(JSON.parse(valor), null, 2);
  } catch {
    return valor;
  }
}
