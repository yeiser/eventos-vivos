import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { NotificacionService } from '../notificaciones/notificacion.service';
import { errorInterceptor } from './error.interceptor';

describe('errorInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  const noti = { error: vi.fn() };

  beforeEach(() => {
    noti.error.mockClear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: NotificacionService, useValue: noti },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('muestra el detalle del ProblemDetails', () => {
    http.get('/api/x').subscribe({ error: () => {} });
    httpMock.expectOne('/api/x').flush({ detail: 'Algo falló' }, { status: 422, statusText: 'Unprocessable' });

    expect(noti.error).toHaveBeenCalledWith('Algo falló');
  });

  it('resume los errores de validación', () => {
    http.get('/api/x').subscribe({ error: () => {} });
    httpMock.expectOne('/api/x').flush(
      { errors: { titulo: ['El título es obligatorio.'] } },
      { status: 400, statusText: 'Bad Request' },
    );

    expect(noti.error).toHaveBeenCalledWith('El título es obligatorio.');
  });

  it('mensaje claro cuando no hay conexión', () => {
    http.get('/api/x').subscribe({ error: () => {} });
    httpMock.expectOne('/api/x').error(new ProgressEvent('error'), { status: 0 });

    expect(noti.error).toHaveBeenCalledWith('No se pudo conectar con el servidor.');
  });
});
