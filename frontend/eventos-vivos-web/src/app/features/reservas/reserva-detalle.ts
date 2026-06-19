import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ReservasApiService } from '../../core/api/reservas-api.service';
import { SessionService } from '../../core/auth/session.service';
import { Reserva } from '../../core/models/reserva.models';
import { NotificacionService } from '../../core/notificaciones/notificacion.service';
import { badgeEstadoReserva, etiquetaEstadoReserva } from '../../shared/format';

@Component({
  selector: 'app-reserva-detalle',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DatePipe],
  templateUrl: './reserva-detalle.html',
})
export class ReservaDetalle {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ReservasApiService);
  private readonly noti = inject(NotificacionService);
  protected readonly session = inject(SessionService);

  protected readonly etiquetaEstadoReserva = etiquetaEstadoReserva;
  protected readonly badgeEstadoReserva = badgeEstadoReserva;

  protected readonly reserva = signal<Reserva | null>(null);
  protected readonly cargando = signal(true);
  protected readonly accionando = signal(false);
  protected readonly id = this.route.snapshot.paramMap.get('id') ?? '';

  protected readonly puedeConfirmar = computed(
    () => this.session.esAdmin() && this.reserva()?.estado === 'pendiente_pago',
  );
  protected readonly puedeCancelar = computed(() => {
    const estado = this.reserva()?.estado;
    return estado === 'pendiente_pago' || estado === 'confirmada';
  });

  constructor() {
    this.cargar();
  }

  protected confirmar(): void {
    this.accionando.set(true);
    this.api.confirmar(this.id).subscribe({
      next: (r) => {
        this.reserva.set(r);
        this.accionando.set(false);
        this.noti.exito(`Pago confirmado. Código ${r.codigo}.`);
      },
      error: () => this.accionando.set(false),
    });
  }

  protected cancelar(): void {
    this.accionando.set(true);
    this.api.cancelar(this.id).subscribe({
      next: (r) => {
        this.reserva.set(r);
        this.accionando.set(false);
        this.noti.exito('Reserva cancelada.');
      },
      error: () => this.accionando.set(false),
    });
  }

  private cargar(): void {
    this.api.obtener(this.id).subscribe({
      next: (r) => {
        this.reserva.set(r);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false),
    });
  }
}
