import { Injectable, computed, signal } from '@angular/core';
import { LoginResponse, Rol, Sesion } from '../models/auth.models';

/**
 * Estado de sesión basado en signals, persistido en localStorage. Única fuente del token y del rol.
 */
@Injectable({ providedIn: 'root' })
export class SessionService {
  private static readonly StorageKey = 'eventosvivos.sesion';

  private readonly _sesion = signal<Sesion | null>(this.cargar());

  readonly sesion = this._sesion.asReadonly();
  readonly estaAutenticado = computed(() => this._sesion() !== null);
  readonly esAdmin = computed(() => this._sesion()?.rol === 'Admin');
  readonly nombreUsuario = computed(() => this._sesion()?.nombreUsuario ?? null);
  readonly rol = computed<Rol | null>(() => this._sesion()?.rol ?? null);

  get token(): string | null {
    return this._sesion()?.token ?? null;
  }

  establecer(login: LoginResponse): void {
    const sesion: Sesion = {
      token: login.token,
      nombreUsuario: login.nombreUsuario,
      rol: login.rol,
      expiraEn: login.expiraEn,
    };
    this._sesion.set(sesion);
    localStorage.setItem(SessionService.StorageKey, JSON.stringify(sesion));
  }

  limpiar(): void {
    this._sesion.set(null);
    localStorage.removeItem(SessionService.StorageKey);
  }

  private cargar(): Sesion | null {
    try {
      const raw = localStorage.getItem(SessionService.StorageKey);
      if (!raw) {
        return null;
      }
      const sesion = JSON.parse(raw) as Sesion;
      if (new Date(sesion.expiraEn).getTime() <= Date.now()) {
        localStorage.removeItem(SessionService.StorageKey);
        return null;
      }
      return sesion;
    } catch {
      return null;
    }
  }
}
