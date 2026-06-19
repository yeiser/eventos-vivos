import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ProblemDetails } from '../models/common.models';
import { NotificacionService } from '../notificaciones/notificacion.service';

/** Traduce los errores HTTP (ProblemDetails RFC 7807) a notificaciones legibles. */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notificaciones = inject(NotificacionService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      notificaciones.error(mensajeDeError(error));
      return throwError(() => error);
    }),
  );
};

function mensajeDeError(error: HttpErrorResponse): string {
  if (error.status === 0) {
    return 'No se pudo conectar con el servidor.';
  }

  const problema = error.error as ProblemDetails | undefined;

  if (problema?.errors) {
    const detalles = Object.values(problema.errors).flat();
    if (detalles.length > 0) {
      return detalles.join(' ');
    }
  }

  return problema?.detail || problema?.title || error.message || 'Ocurrió un error inesperado.';
}
