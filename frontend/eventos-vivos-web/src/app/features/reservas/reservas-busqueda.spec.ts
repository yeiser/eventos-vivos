import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { environment } from '../../../environments/environment';
import { ReservasBusqueda } from './reservas-busqueda';

const API = environment.apiBaseUrl;
const resumen = (over = {}) => ({
  id: 'r1', eventoId: 'e1', eventoTitulo: 'Jazz', cantidad: 2, nombreComprador: 'Ana',
  emailComprador: 'ana@x.com', estado: 'pendiente_pago', codigo: null, fechaReserva: '2026-06-01T10:00:00Z', ...over,
});

describe('ReservasBusqueda', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  function crear(items = [resumen()], total = 1) {
    const fixture = TestBed.createComponent(ReservasBusqueda);
    fixture.detectChanges();
    httpMock.expectOne((r) => r.url === `${API}/reservas`).flush({ items, total });
    return fixture.componentInstance as any;
  }

  it('busca al iniciar y expone resultados + paginación', () => {
    const cmp = crear([resumen(), resumen({ id: 'r2' })], 16);
    expect(cmp.items()).toHaveLength(2);
    expect(cmp.total()).toBe(16);
    expect(cmp.totalPaginas()).toBe(2); // ceil(16/15)
  });

  it('cancelable solo para pendiente/confirmada', () => {
    const cmp = crear();
    expect(cmp.cancelable({ estado: 'pendiente_pago' })).toBe(true);
    expect(cmp.cancelable({ estado: 'confirmada' })).toBe(true);
    expect(cmp.cancelable({ estado: 'cancelada' })).toBe(false);
  });

  it('confirmar actualiza la fila con el código devuelto', () => {
    const cmp = crear();
    cmp.confirmar(resumen());
    httpMock.expectOne(`${API}/reservas/r1/confirmacion`).flush({ estado: 'confirmada', codigo: '123456' });
    const fila = cmp.items().find((x: any) => x.id === 'r1');
    expect(fila.estado).toBe('confirmada');
    expect(fila.codigo).toBe('123456');
    expect(cmp.accionandoId()).toBeNull();
  });

  it('cancelar marca la reserva como cancelada', () => {
    const cmp = crear();
    cmp.cancelar(resumen());
    httpMock.expectOne(`${API}/reservas/r1/cancelacion`).flush({ estado: 'cancelada', codigo: null });
    expect(cmp.items().find((x: any) => x.id === 'r1').estado).toBe('cancelada');
  });
});
