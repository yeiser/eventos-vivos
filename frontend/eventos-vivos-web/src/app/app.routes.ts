import { Routes } from '@angular/router';
import { adminGuard, authGuard } from './core/auth/guards';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login').then((m) => m.Login),
  },
  {
    path: '',
    loadComponent: () => import('./shared/layout/layout').then((m) => m.Layout),
    canActivate: [authGuard],
    children: [
      {
        path: 'eventos',
        loadComponent: () => import('./features/eventos/eventos-list').then((m) => m.EventosList),
      },
      {
        path: 'eventos/nuevo',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/eventos/evento-form').then((m) => m.EventoForm),
      },
      {
        path: 'eventos/:id',
        loadComponent: () => import('./features/eventos/evento-detalle').then((m) => m.EventoDetalle),
      },
      {
        path: 'eventos/:id/reservar',
        loadComponent: () => import('./features/eventos/reservar').then((m) => m.Reservar),
      },
      {
        path: 'eventos/:id/reporte',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/eventos/evento-reporte').then((m) => m.EventoReporte),
      },
      {
        path: 'reservas',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('./features/reservas/reservas-busqueda').then((m) => m.ReservasBusqueda),
      },
      {
        path: 'reservas/nueva',
        loadComponent: () => import('./features/reservas/nueva-reserva').then((m) => m.NuevaReserva),
      },
      {
        path: 'reservas/:id',
        loadComponent: () => import('./features/reservas/reserva-detalle').then((m) => m.ReservaDetalle),
      },
      {
        path: 'auditoria',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/auditoria/auditoria').then((m) => m.Auditoria),
      },
      { path: '', redirectTo: 'eventos', pathMatch: 'full' },
    ],
  },
  { path: '**', redirectTo: '' },
];
