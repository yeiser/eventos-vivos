import { NotificacionService } from './notificacion.service';

describe('NotificacionService', () => {
  let svc: NotificacionService;

  beforeEach(() => {
    svc = new NotificacionService();
  });

  it('exito() agrega una notificación de tipo success', () => {
    svc.exito('hecho');
    expect(svc.items()).toHaveLength(1);
    expect(svc.items()[0]).toMatchObject({ tipo: 'success', mensaje: 'hecho' });
  });

  it('error() agrega una de tipo danger', () => {
    svc.error('ups');
    expect(svc.items()[0].tipo).toBe('danger');
  });

  it('asigna ids incrementales únicos', () => {
    svc.mostrar('info', 'a', 0);
    svc.mostrar('warning', 'b', 0);
    const [a, b] = svc.items();
    expect(a.id).not.toBe(b.id);
  });

  it('cerrar() elimina la notificación por id', () => {
    svc.mostrar('info', 'a', 0);
    const id = svc.items()[0].id;
    svc.cerrar(id);
    expect(svc.items()).toHaveLength(0);
  });

  it('auto-cierra tras la duración indicada', () => {
    vi.useFakeTimers();
    try {
      svc.mostrar('info', 'temporal', 5000);
      expect(svc.items()).toHaveLength(1);
      vi.advanceTimersByTime(5000);
      expect(svc.items()).toHaveLength(0);
    } finally {
      vi.useRealTimers();
    }
  });
});
