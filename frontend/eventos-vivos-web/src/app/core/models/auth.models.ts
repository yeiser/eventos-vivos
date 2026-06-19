export type Rol = 'Admin' | 'Usuario';

export interface LoginRequest {
  nombreUsuario: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiraEn: string;
  nombreUsuario: string;
  rol: Rol;
}

/** Sesión persistida del usuario autenticado. */
export interface Sesion {
  token: string;
  nombreUsuario: string;
  rol: Rol;
  expiraEn: string;
}
