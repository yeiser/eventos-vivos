import { Injectable, signal } from '@angular/core';

export type TipoNotificacion = 'success' | 'danger' | 'info' | 'warning';

export interface Notificacion {
  id: number;
  tipo: TipoNotificacion;
  mensaje: string;
}

/** Notificaciones tipo toast basadas en signals (se renderizan con clases de Metronic). */
@Injectable({ providedIn: 'root' })
export class NotificacionService {
  private readonly _items = signal<Notificacion[]>([]);
  private secuencia = 0;

  readonly items = this._items.asReadonly();

  mostrar(tipo: TipoNotificacion, mensaje: string, duracionMs = 5000): void {
    const id = ++this.secuencia;
    this._items.update((lista) => [...lista, { id, tipo, mensaje }]);
    if (duracionMs > 0) {
      setTimeout(() => this.cerrar(id), duracionMs);
    }
  }

  exito(mensaje: string): void {
    this.mostrar('success', mensaje);
  }

  error(mensaje: string): void {
    this.mostrar('danger', mensaje);
  }

  cerrar(id: number): void {
    this._items.update((lista) => lista.filter((n) => n.id !== id));
  }
}
