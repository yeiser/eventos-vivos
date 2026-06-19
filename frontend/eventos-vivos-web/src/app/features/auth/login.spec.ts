import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Login } from './login';

describe('Login', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  it('se renderiza con el formulario de acceso', () => {
    const fixture = TestBed.createComponent(Login);
    fixture.detectChanges();

    const html = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(html).toContain('Ingresar');
    expect((fixture.nativeElement as HTMLElement).querySelector('input[formControlName="nombreUsuario"]')).toBeTruthy();
  });

  it('el botón no inicia carga con el formulario vacío', () => {
    const fixture = TestBed.createComponent(Login);
    fixture.detectChanges();
    const boton = (fixture.nativeElement as HTMLElement).querySelector('button[type="submit"]') as HTMLButtonElement;

    boton.click();
    fixture.detectChanges();

    // Formulario inválido → no entra en estado "cargando".
    expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('Ingresando');
  });
});
