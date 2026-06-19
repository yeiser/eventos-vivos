import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { environment } from '../../../environments/environment';
import { EventoForm } from './evento-form';

describe('EventoForm', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  function crear() {
    const fixture = TestBed.createComponent(EventoForm);
    fixture.detectChanges();
    httpMock.expectOne(`${environment.apiBaseUrl}/venues`).flush([]); // venues del constructor
    return fixture.componentInstance as any;
  }

  it('el formulario vacío es inválido', () => {
    const cmp = crear();
    expect(cmp.form.invalid).toBe(true);
  });

  it('un título corto es inválido (mínimo 5)', () => {
    const cmp = crear();
    cmp.form.patchValue({ titulo: 'abc' });
    expect(cmp.form.controls.titulo.invalid).toBe(true);
  });

  it('un formulario completo y válido pasa', () => {
    const cmp = crear();
    cmp.form.patchValue({
      titulo: 'Concierto de Jazz',
      descripcion: 'Una noche de jazz con artistas invitados.',
      venueId: 1,
      capacidadMaxima: 100,
      precio: 50,
      tipo: 'concierto',
      fechaInicio: '2027-01-10T19:00',
      fechaFin: '2027-01-10T22:00',
    });
    expect(cmp.form.valid).toBe(true);
  });

  afterEach(() => httpMock.verify());
});
