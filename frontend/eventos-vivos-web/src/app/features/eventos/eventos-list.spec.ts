import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { environment } from '../../../environments/environment';
import { EventosList } from './eventos-list';

const API = environment.apiBaseUrl;
const evento = (over = {}) => ({
  id: 'e1', titulo: 'Jazz', venueId: 1, fechaInicio: '2026-07-01T20:00:00Z',
  fechaFin: '2026-07-01T22:00:00Z', precio: 50, tipo: 'concierto', estado: 'activo',
  capacidadMaxima: 100, entradasDisponibles: 80, ...over,
});

describe('EventosList', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  function crear() {
    const fixture = TestBed.createComponent(EventosList);
    fixture.detectChanges();
    httpMock.expectOne(`${API}/venues`).flush([{ id: 1, nombre: 'Teatro Colón', ciudad: 'Bogotá', capacidad: 100 }]);
    httpMock.expectOne((r) => r.url === `${API}/eventos`).flush({ items: [evento()], total: 25 });
    return fixture.componentInstance as any;
  }

  it('carga eventos y venues y calcula la paginación', () => {
    const cmp = crear();
    expect(cmp.eventos()).toHaveLength(1);
    expect(cmp.total()).toBe(25);
    expect(cmp.totalPaginas()).toBe(3); // ceil(25/10)
  });

  it('nombreVenue resuelve el nombre o cae a #id', () => {
    const cmp = crear();
    expect(cmp.nombreVenue(1)).toBe('Teatro Colón');
    expect(cmp.nombreVenue(99)).toBe('#99');
  });

  it('irPagina dispara una nueva búsqueda dentro del rango', () => {
    const cmp = crear();
    cmp.irPagina(2);
    httpMock.expectOne((r) => r.url === `${API}/eventos`).flush({ items: [], total: 25 });
    expect(cmp.pagina()).toBe(2);
    cmp.irPagina(99); // fuera de rango: no hace nada
    httpMock.expectNone((r: any) => r.url === `${API}/eventos`);
  });

  it('limpiar reinicia filtros y vuelve a la página 1', () => {
    const cmp = crear();
    cmp.irPagina(2);
    httpMock.expectOne((r) => r.url === `${API}/eventos`).flush({ items: [], total: 25 });
    cmp.limpiar();
    httpMock.expectOne((r) => r.url === `${API}/eventos`).flush({ items: [], total: 0 });
    expect(cmp.pagina()).toBe(1);
  });
});
