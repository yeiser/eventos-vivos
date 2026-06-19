import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { NotificacionService } from '../notificaciones/notificacion.service';
import { SessionService } from './session.service';

/** Exige usuario autenticado; si no, redirige a /login. */
export const authGuard: CanActivateFn = () => {
  const session = inject(SessionService);
  const router = inject(Router);
  return session.estaAutenticado() ? true : router.createUrlTree(['/login']);
};

/** Exige rol Admin; si no, redirige al listado y avisa. */
export const adminGuard: CanActivateFn = () => {
  const session = inject(SessionService);
  const router = inject(Router);
  if (session.esAdmin()) {
    return true;
  }
  inject(NotificacionService).error('Requiere permisos de administrador.');
  return router.createUrlTree(['/eventos']);
};
