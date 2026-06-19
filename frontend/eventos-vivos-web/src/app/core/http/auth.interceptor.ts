import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { SessionService } from '../auth/session.service';

/**
 * Adjunta el token JWT como Bearer y, ante un 401 (token ausente/expirado), limpia la sesión y
 * redirige a /login. No redirige en el propio endpoint de login (un 401 ahí es credencial inválida).
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const session = inject(SessionService);
  const router = inject(Router);

  const token = session.token;
  const peticion = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(peticion).pipe(
    catchError((error: HttpErrorResponse) => {
      const esLogin = req.url.includes('/auth/login');
      if (error.status === 401 && !esLogin) {
        session.limpiar();
        router.navigate(['/login']);
      }
      return throwError(() => error);
    }),
  );
};
