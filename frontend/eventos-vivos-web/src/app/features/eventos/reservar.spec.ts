import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { environment } from '../../../environments/environment';
import { Evento } from '../../core/models/evento.models';
import { Reservar } from './reservar';

const eventoBase = (over: Partial<Evento>): Evento => ({
  id: 'evt-1',
  titulo: 'Evento',
  descripcion: 'desc',
  venueId: 1,
  capacidadMaxima: 100,
  fechaInicio: new Date(Date.now() + 10 * 24 * 3600_000).toISOString(),
  fechaFin: new Date(Date.now() + 10 * 24 * 3600_000 + 7200_000).toISOString(),
  precio: 50,
  tipo: 'concierto',
  estado: 'activo',
  entradasVendidas: 0,
  entradasDisponibles: 100,
  auditoria: { creadoPor: 'admin', fechaCreacion: '', modificadoPor: null, fechaUltimaModificacion: null },
  ...over,
});

describe('Reservar (límite efectivo A-02)', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'evt-1' } } } },
      ],
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  function crearCon(evento: Evento) {
    const fixture = TestBed.createComponent(Reservar);
    fixture.detectChanges();
    httpMock.expectOne(`${environment.apiBaseUrl}/eventos/evt-1`).flush(evento);
    return fixture.componentInstance as any;
  }

  const enHoras = (h: number) => new Date(Date.now() + h * 3600_000).toISOString();

  it('evento lejano y precio normal → límite = disponibles', () => {
    const cmp = crearCon(eventoBase({ entradasDisponibles: 80 }));
    expect(cmp.limite()).toBe(80);
  });

  it('evento en menos de 24h → máximo 5 (RF-03)', () => {
    const cmp = crearCon(eventoBase({ fechaInicio: enHoras(10) }));
    expect(cmp.limite()).toBe(5);
  });

  it('precio > 100 → máximo 10 (RN05)', () => {
    const cmp = crearCon(eventoBase({ precio: 150 }));
    expect(cmp.limite()).toBe(10);
  });

  it('en menos de 24h y precio alto → gana el más restrictivo (5)', () => {
    const cmp = crearCon(eventoBase({ fechaInicio: enHoras(10), precio: 150 }));
    expect(cmp.limite()).toBe(5);
  });

  it('a menos de 1 hora → no se puede reservar (RN04)', () => {
    const cmp = crearCon(eventoBase({ fechaInicio: enHoras(0.5) }));
    expect(cmp.limite()).toBe(0);
  });
});
