import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { NotificacionService } from '../notificaciones/notificacion.service';
import { adminGuard, authGuard } from './guards';
import { SessionService } from './session.service';

describe('guards', () => {
  const URL_TREE = { __urlTree: true } as never;
  let router: { createUrlTree: ReturnType<typeof vi.fn> };
  let session: { estaAutenticado: ReturnType<typeof vi.fn>; esAdmin: ReturnType<typeof vi.fn> };
  let noti: { error: ReturnType<typeof vi.fn> };

  const run = (guard: typeof authGuard) =>
    TestBed.runInInjectionContext(() => guard({} as never, {} as never));

  beforeEach(() => {
    router = { createUrlTree: vi.fn().mockReturnValue(URL_TREE) };
    session = { estaAutenticado: vi.fn(), esAdmin: vi.fn() };
    noti = { error: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        { provide: Router, useValue: router },
        { provide: SessionService, useValue: session },
        { provide: NotificacionService, useValue: noti },
      ],
    });
  });

  describe('authGuard', () => {
    it('permite el acceso si hay sesión', () => {
      session.estaAutenticado.mockReturnValue(true);
      expect(run(authGuard)).toBe(true);
    });

    it('redirige a /login si no hay sesión', () => {
      session.estaAutenticado.mockReturnValue(false);
      expect(run(authGuard)).toBe(URL_TREE);
      expect(router.createUrlTree).toHaveBeenCalledWith(['/login']);
    });
  });

  describe('adminGuard', () => {
    it('permite el acceso a un Admin', () => {
      session.esAdmin.mockReturnValue(true);
      expect(run(adminGuard)).toBe(true);
      expect(noti.error).not.toHaveBeenCalled();
    });

    it('a un no-Admin lo redirige a /eventos y avisa', () => {
      session.esAdmin.mockReturnValue(false);
      expect(run(adminGuard)).toBe(URL_TREE);
      expect(noti.error).toHaveBeenCalledOnce();
      expect(router.createUrlTree).toHaveBeenCalledWith(['/eventos']);
    });
  });
});
