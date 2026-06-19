import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { environment } from '../../../environments/environment';
import { SessionService } from '../../core/auth/session.service';
import { EventoDetalle } from './evento-detalle';

const API = environment.apiBaseUrl;
const futuro = () => new Date(Date.now() + 3_600_000).toISOString();
const evento = (over = {}) => ({
  id: 'e1', titulo: 'Jazz', descripcion: 'desc', venueId: 1, capacidadMaxima: 100,
  fechaInicio: '2026-07-01T20:00:00Z', fechaFin: '2026-07-01T22:00:00Z', precio: 50,
  tipo: 'concierto', estado: 'activo', entradasVendidas: 0, entradasDisponibles: 100,
  auditoria: { creadoPor: 'admin', fechaCreacion: '', modificadoPor: null, fechaUltimaModificacion: null },
  ...over,
});
const reserva = (over = {}) => ({
  id: 'r1', eventoId: 'e1', cantidad: 2, nombreComprador: 'Ana', emailComprador: 'ana@x.com',
  estado: 'pendiente_pago', codigo: null, fechaReserva: '2026-06-01T10:00:00Z',
  fechaConfirmacion: null, fechaCancelacion: null,
  auditoria: { creadoPor: 'admin', fechaCreacion: '', modificadoPor: null, fechaUltimaModificacion: null },
  ...over,
});

describe('EventoDetalle', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'e1' } } } },
      ],
    });
    httpMock = TestBed.inject(HttpTestingController);
    TestBed.inject(SessionService).establecer({ token: 't', expiraEn: futuro(), nombreUsuario: 'admin', rol: 'Admin' });
  });

  afterEach(() => httpMock.verify());

  function crear() {
    const fixture = TestBed.createComponent(EventoDetalle);
    fixture.detectChanges();
    httpMock.expectOne(`${API}/eventos/e1`).flush(evento());
    httpMock.expectOne(`${API}/eventos/e1/reservas`).flush([reserva()]); // rama admin
    return fixture.componentInstance as any;
  }

  it('carga el evento y sus reservas (Admin)', () => {
    const cmp = crear();
    expect(cmp.evento().id).toBe('e1');
    expect(cmp.reservas()).toHaveLength(1);
    expect(cmp.cargando()).toBe(false);
  });

  it('cancelable solo para pendiente/confirmada', () => {
    const cmp = crear();
    expect(cmp.cancelable({ estado: 'pendiente_pago' })).toBe(true);
    expect(cmp.cancelable({ estado: 'cancelada' })).toBe(false);
  });

  it('confirmar() reemplaza la reserva y refresca el evento', () => {
    const cmp = crear();
    cmp.confirmar(reserva());
    httpMock.expectOne(`${API}/reservas/r1/confirmacion`).flush(reserva({ estado: 'confirmada', codigo: '123456' }));
    httpMock.expectOne(`${API}/eventos/e1`).flush(evento({ entradasDisponibles: 98 })); // refrescarEvento
    expect(cmp.reservas()[0].estado).toBe('confirmada');
    expect(cmp.accionandoId()).toBeNull();
  });
});
