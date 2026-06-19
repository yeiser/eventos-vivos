import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { environment } from '../../../environments/environment';
import { NuevaReserva } from './nueva-reserva';

const API = environment.apiBaseUrl;
const lejano = () => new Date(Date.now() + 48 * 3_600_000).toISOString();
const evento = (over = {}) => ({
  id: 'e1', titulo: 'Jazz', venueId: 1, fechaInicio: lejano(), fechaFin: lejano(), precio: 50,
  tipo: 'concierto', estado: 'activo', capacidadMaxima: 100, entradasDisponibles: 80, ...over,
});

describe('NuevaReserva', () => {
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => httpMock.verify());

  function crear(items = [evento()]) {
    const fixture = TestBed.createComponent(NuevaReserva);
    fixture.detectChanges();
    httpMock.expectOne((r) => r.url === `${API}/eventos`).flush({ items, total: items.length });
    return fixture.componentInstance as any;
  }

  it('carga eventos activos al iniciar', () => {
    const cmp = crear();
    expect(cmp.eventos()).toHaveLength(1);
  });

  it('seleccionarEvento fija el evento y calcula el límite', () => {
    const cmp = crear();
    cmp.form.controls.eventoId.setValue('e1');
    cmp.seleccionarEvento();
    expect(cmp.eventoSel().id).toBe('e1');
    expect(cmp.limite()).toBe(80); // lejano + precio normal => disponibles
  });

  it('reservar() inválido no llama a la API', () => {
    const cmp = crear();
    cmp.reservar(); // formulario vacío
    httpMock.expectNone((r: any) => r.method === 'POST');
    expect(cmp.form.touched).toBe(true);
  });

  it('reservar() válido crea la reserva y navega al detalle', () => {
    const cmp = crear();
    const nav = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    cmp.form.setValue({ eventoId: 'e1', cantidad: 2, nombreComprador: 'Ana', emailComprador: 'ana@x.com' });
    cmp.seleccionarEvento();

    cmp.reservar();
    const req = httpMock.expectOne(`${API}/eventos/e1/reservas`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ cantidad: 2, nombreComprador: 'Ana', emailComprador: 'ana@x.com' });
    req.flush({ id: 'r9' });

    expect(nav).toHaveBeenCalledWith(['/reservas', 'r9']);
  });
});
