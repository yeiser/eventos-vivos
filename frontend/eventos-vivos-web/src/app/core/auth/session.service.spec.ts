import { LoginResponse, Rol } from '../models/auth.models';
import { SessionService } from './session.service';

const futuro = () => new Date(Date.now() + 3_600_000).toISOString();
const login = (rol: Rol = 'Admin'): LoginResponse => ({
  token: 'tok',
  expiraEn: futuro(),
  nombreUsuario: 'admin',
  rol,
});

describe('SessionService', () => {
  beforeEach(() => localStorage.clear());

  it('establecer guarda la sesión, el token y el rol', () => {
    const s = new SessionService();
    s.establecer(login('Admin'));

    expect(s.estaAutenticado()).toBe(true);
    expect(s.token).toBe('tok');
    expect(s.esAdmin()).toBe(true);
    expect(s.nombreUsuario()).toBe('admin');
  });

  it('un usuario no admin no es admin', () => {
    const s = new SessionService();
    s.establecer(login('Usuario'));
    expect(s.esAdmin()).toBe(false);
  });

  it('limpiar borra la sesión', () => {
    const s = new SessionService();
    s.establecer(login());
    s.limpiar();

    expect(s.estaAutenticado()).toBe(false);
    expect(s.token).toBeNull();
  });

  it('carga una sesión persistida no expirada', () => {
    localStorage.setItem(
      'eventosvivos.sesion',
      JSON.stringify({ token: 't2', nombreUsuario: 'u', rol: 'Usuario', expiraEn: futuro() }),
    );

    const s = new SessionService();
    expect(s.estaAutenticado()).toBe(true);
    expect(s.esAdmin()).toBe(false);
  });

  it('ignora una sesión expirada', () => {
    localStorage.setItem(
      'eventosvivos.sesion',
      JSON.stringify({ token: 't3', nombreUsuario: 'u', rol: 'Admin', expiraEn: new Date(Date.now() - 1000).toISOString() }),
    );

    const s = new SessionService();
    expect(s.estaAutenticado()).toBe(false);
  });
});
