import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { environment } from '../../../environments/environment';
import { SessionService } from '../../core/auth/session.service';
import { ReservaDetalle } from './reserva-detalle';

const API = environment.apiBaseUrl;
const futuro = () => new Date(Date.now() + 3_600_000).toISOString();
const reserva = (over = {}) => ({
  id: 'r1', eventoId: 'e1', cantidad: 2, nombreComprador: 'Ana', emailComprador: 'ana@x.com',
  estado: 'pendiente_pago', codigo: null, fechaReserva: '2026-06-01T10:00:00Z',
  fechaConfirmacion: null, fechaCancelacion: null,
  auditoria: { creadoPor: 'admin', fechaCreacion: '', modificadoPor: null, fechaUltimaModificacion: null },
  ...over,
});

describe('ReservaDetalle', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'r1' } } } },
      ],
    });
    httpMock = TestBed.inject(HttpTestingController);
    TestBed.inject(SessionService).establecer({ token: 't', expiraEn: futuro(), nombreUsuario: 'admin', rol: 'Admin' });
  });

  afterEach(() => httpMock.verify());

  function crear(over = {}) {
    const fixture = TestBed.createComponent(ReservaDetalle);
    fixture.detectChanges();
    httpMock.expectOne(`${API}/reservas/r1`).flush(reserva(over));
    return fixture.componentInstance as any;
  }

  it('carga la reserva y deja confirmar/cancelar disponibles para un Admin', () => {
    const cmp = crear();
    expect(cmp.reserva().id).toBe('r1');
    expect(cmp.cargando()).toBe(false);
    expect(cmp.puedeConfirmar()).toBe(true); // admin + pendiente_pago
    expect(cmp.puedeCancelar()).toBe(true);
  });

  it('no permite confirmar una reserva ya confirmada', () => {
    const cmp = crear({ estado: 'confirmada' });
    expect(cmp.puedeConfirmar()).toBe(false);
    expect(cmp.puedeCancelar()).toBe(true);
  });

  it('confirmar() actualiza el estado y notifica', () => {
    const cmp = crear();
    cmp.confirmar();
    httpMock.expectOne(`${API}/reservas/r1/confirmacion`).flush(reserva({ estado: 'confirmada', codigo: '123456' }));
    expect(cmp.reserva().estado).toBe('confirmada');
    expect(cmp.accionando()).toBe(false);
  });

  it('cancelar() marca cancelada', () => {
    const cmp = crear();
    cmp.cancelar();
    httpMock.expectOne(`${API}/reservas/r1/cancelacion`).flush(reserva({ estado: 'cancelada' }));
    expect(cmp.reserva().estado).toBe('cancelada');
  });
});
