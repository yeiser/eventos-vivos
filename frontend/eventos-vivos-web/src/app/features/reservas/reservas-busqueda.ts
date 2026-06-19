import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ReservasApiService } from '../../core/api/reservas-api.service';
import {
  ESTADOS_RESERVA,
  EstadoReserva,
  ReservaFiltro,
  ReservaResumen,
} from '../../core/models/reserva.models';
import { NotificacionService } from '../../core/notificaciones/notificacion.service';
import { badgeEstadoReserva, etiquetaEstadoReserva } from '../../shared/format';

@Component({
  selector: 'app-reservas-busqueda',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, DatePipe],
  templateUrl: './reservas-busqueda.html',
})
export class ReservasBusqueda {
  private readonly api = inject(ReservasApiService);
  private readonly noti = inject(NotificacionService);

  protected readonly estados = ESTADOS_RESERVA;
  protected readonly etiquetaEstadoReserva = etiquetaEstadoReserva;
  protected readonly badgeEstadoReserva = badgeEstadoReserva;

  protected readonly cargando = signal(false);
  protected readonly items = signal<ReservaResumen[]>([]);
  protected readonly total = signal(0);
  protected readonly pagina = signal(1);
  protected readonly accionandoId = signal<string | null>(null);
  private readonly tamano = 15;

  protected readonly totalPaginas = computed(() => Math.max(1, Math.ceil(this.total() / this.tamano)));

  protected readonly filtros = new FormGroup({
    codigo: new FormControl('', { nonNullable: true }),
    nombreComprador: new FormControl('', { nonNullable: true }),
    estado: new FormControl<EstadoReserva | ''>('', { nonNullable: true }),
  });

  constructor() {
    this.buscar();
  }

  protected aplicar(): void {
    this.pagina.set(1);
    this.buscar();
  }

  protected limpiar(): void {
    this.filtros.reset({ codigo: '', nombreComprador: '', estado: '' });
    this.aplicar();
  }

  protected irPagina(p: number): void {
    if (p >= 1 && p <= this.totalPaginas()) {
      this.pagina.set(p);
      this.buscar();
    }
  }

  protected cancelable(r: ReservaResumen): boolean {
    return r.estado === 'pendiente_pago' || r.estado === 'confirmada';
  }

  protected confirmar(r: ReservaResumen): void {
    this.accionandoId.set(r.id);
    this.api.confirmar(r.id).subscribe({
      next: (d) => {
        this.actualizar(r.id, d.estado, d.codigo);
        this.accionandoId.set(null);
        this.noti.exito(`Pago confirmado. Código ${d.codigo}.`);
      },
      error: () => this.accionandoId.set(null),
    });
  }

  protected cancelar(r: ReservaResumen): void {
    this.accionandoId.set(r.id);
    this.api.cancelar(r.id).subscribe({
      next: (d) => {
        this.actualizar(r.id, d.estado, d.codigo);
        this.accionandoId.set(null);
        this.noti.exito('Reserva cancelada.');
      },
      error: () => this.accionandoId.set(null),
    });
  }

  private actualizar(id: string, estado: EstadoReserva, codigo: string | null): void {
    this.items.update((lista) => lista.map((x) => (x.id === id ? { ...x, estado, codigo } : x)));
  }

  private buscar(): void {
    const f = this.filtros.getRawValue();
    const filtro: ReservaFiltro = {
      pagina: this.pagina(),
      tamanoPagina: this.tamano,
      codigo: f.codigo || undefined,
      nombreComprador: f.nombreComprador || undefined,
      estado: f.estado || undefined,
    };

    this.cargando.set(true);
    this.api.buscar(filtro).subscribe({
      next: (r) => {
        this.items.set(r.items);
        this.total.set(r.total);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false),
    });
  }
}
