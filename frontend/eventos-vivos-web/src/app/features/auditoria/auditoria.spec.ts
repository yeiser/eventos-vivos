import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { AuditLog } from '../../core/models/auditoria.models';
import { Auditoria } from './auditoria';

const log = (over: Partial<AuditLog> = {}): AuditLog => ({
  id: 'a1',
  entidad: 'Evento',
  entidadId: 'evt-1',
  accion: 'crear',
  usuario: 'admin',
  fecha: new Date().toISOString(),
  valoresAnteriores: null,
  valoresNuevos: '{"Titulo":"Jazz"}',
  camposModificados: null,
  traceId: 'trace-1',
  ipOrigen: '127.0.0.1',
  ...over,
});

describe('Auditoria', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  function crear() {
    const fixture = TestBed.createComponent(Auditoria);
    fixture.detectChanges();
    const req = httpMock.expectOne((r) => r.url === `${environment.apiBaseUrl}/auditoria`);
    req.flush({ items: [log()], pagina: 1, tamanoPagina: 15, total: 1, totalPaginas: 1 });
    return { fixture, cmp: fixture.componentInstance as any };
  }

  it('carga el audit trail al iniciar', () => {
    const { cmp } = crear();
    expect(cmp.items().length).toBe(1);
    expect(cmp.total()).toBe(1);
  });

  it('alternarDetalle expande y colapsa una fila', () => {
    const { cmp } = crear();
    expect(cmp.expandido()).toBeNull();
    cmp.alternarDetalle('a1');
    expect(cmp.expandido()).toBe('a1');
    cmp.alternarDetalle('a1');
    expect(cmp.expandido()).toBeNull();
  });
});
