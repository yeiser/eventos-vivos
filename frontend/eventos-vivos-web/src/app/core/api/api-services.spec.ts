import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { AuditoriaApiService } from './auditoria-api.service';
import { EventosApiService } from './eventos-api.service';
import { ReservasApiService } from './reservas-api.service';
import { VenuesApiService } from './venues-api.service';

const API = environment.apiBaseUrl;

describe('servicios de API', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  describe('EventosApiService', () => {
    let svc: EventosApiService;
    beforeEach(() => (svc = TestBed.inject(EventosApiService)));

    it('listar() arma query params y omite vacíos/nulos', () => {
      svc.listar({ titulo: 'jazz', pagina: 2, tipo: undefined } as never).subscribe();
      const req = httpMock.expectOne((r) => r.url === `${API}/eventos`);
      expect(req.request.params.get('titulo')).toBe('jazz');
      expect(req.request.params.get('pagina')).toBe('2');
      expect(req.request.params.has('tipo')).toBe(false);
      req.flush({ items: [], total: 0 });
    });

    it('obtener() hace GET por id', () => {
      svc.obtener('e1').subscribe();
      httpMock.expectOne(`${API}/eventos/e1`).flush({});
    });

    it('crear() hace POST con el body', () => {
      const body = { titulo: 'x' } as never;
      svc.crear(body).subscribe();
      const req = httpMock.expectOne(`${API}/eventos`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toBe(body);
      req.flush({});
    });

    it('reporte(), reservar() y reservasDeEvento() apuntan a las rutas correctas', () => {
      svc.reporte('e1').subscribe();
      httpMock.expectOne(`${API}/eventos/e1/reporte`).flush({});

      svc.reservar('e1', { cantidad: 2 } as never).subscribe();
      const r = httpMock.expectOne(`${API}/eventos/e1/reservas`);
      expect(r.request.method).toBe('POST');
      r.flush({});

      svc.reservasDeEvento('e1').subscribe();
      httpMock.expectOne(`${API}/eventos/e1/reservas`).flush([]);
    });
  });

  describe('ReservasApiService', () => {
    let svc: ReservasApiService;
    beforeEach(() => (svc = TestBed.inject(ReservasApiService)));

    it('buscar() arma params', () => {
      svc.buscar({ codigo: '123456', estado: '' } as never).subscribe();
      const req = httpMock.expectOne((r) => r.url === `${API}/reservas`);
      expect(req.request.params.get('codigo')).toBe('123456');
      expect(req.request.params.has('estado')).toBe(false);
      req.flush({ items: [], total: 0 });
    });

    it('obtener/confirmar/cancelar', () => {
      svc.obtener('r1').subscribe();
      httpMock.expectOne(`${API}/reservas/r1`).flush({});

      svc.confirmar('r1').subscribe();
      expect(httpMock.expectOne(`${API}/reservas/r1/confirmacion`).request.method).toBe('POST');

      svc.cancelar('r1').subscribe();
      expect(httpMock.expectOne(`${API}/reservas/r1/cancelacion`).request.method).toBe('POST');
    });
  });

  describe('VenuesApiService', () => {
    it('listar() hace GET a /venues', () => {
      TestBed.inject(VenuesApiService).listar().subscribe();
      httpMock.expectOne(`${API}/venues`).flush([]);
    });
  });

  describe('AuditoriaApiService', () => {
    it('listar() arma params y pega a /auditoria', () => {
      TestBed.inject(AuditoriaApiService).listar({ entidad: 'Evento', usuario: '' } as never).subscribe();
      const req = httpMock.expectOne((r) => r.url === `${API}/auditoria`);
      expect(req.request.params.get('entidad')).toBe('Evento');
      expect(req.request.params.has('usuario')).toBe(false);
      req.flush({ items: [], total: 0 });
    });
  });
});
