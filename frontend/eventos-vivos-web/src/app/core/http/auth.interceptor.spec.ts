import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { SessionService } from '../auth/session.service';
import { authInterceptor } from './auth.interceptor';

const futuro = () => new Date(Date.now() + 3_600_000).toISOString();

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let session: SessionService;
  const router = { navigate: vi.fn(), createUrlTree: vi.fn() };

  beforeEach(() => {
    localStorage.clear();
    router.navigate.mockClear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        SessionService,
        { provide: Router, useValue: router },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    session = TestBed.inject(SessionService);
  });

  afterEach(() => httpMock.verify());

  it('añade el header Authorization cuando hay token', () => {
    session.establecer({ token: 'abc', expiraEn: futuro(), nombreUsuario: 'a', rol: 'Admin' });

    http.get('/api/x').subscribe();

    const req = httpMock.expectOne('/api/x');
    expect(req.request.headers.get('Authorization')).toBe('Bearer abc');
    req.flush({});
  });

  it('no añade header cuando no hay token', () => {
    http.get('/api/x').subscribe();

    const req = httpMock.expectOne('/api/x');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('ante un 401 limpia la sesión y redirige a /login', () => {
    session.establecer({ token: 'abc', expiraEn: futuro(), nombreUsuario: 'a', rol: 'Admin' });

    http.get('/api/protegido').subscribe({ next: () => {}, error: () => {} });
    httpMock.expectOne('/api/protegido').flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(session.estaAutenticado()).toBe(false);
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('un 401 en el propio login NO redirige', () => {
    http.post('/api/v1/auth/login', {}).subscribe({ next: () => {}, error: () => {} });
    httpMock.expectOne('/api/v1/auth/login').flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(router.navigate).not.toHaveBeenCalled();
  });
});
