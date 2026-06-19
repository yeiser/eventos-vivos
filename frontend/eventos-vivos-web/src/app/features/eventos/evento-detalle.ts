import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { EventosApiService } from '../../core/api/eventos-api.service';
import { ReservasApiService } from '../../core/api/reservas-api.service';
import { SessionService } from '../../core/auth/session.service';
import { Evento } from '../../core/models/evento.models';
import { Reserva } from '../../core/models/reserva.models';
import { NotificacionService } from '../../core/notificaciones/notificacion.service';
import {
  badgeEstadoEvento,
  badgeEstadoReserva,
  etiquetaEstadoEvento,
  etiquetaEstadoReserva,
  etiquetaTipo,
} from '../../shared/format';

@Component({
  selector: 'app-evento-detalle',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DatePipe, DecimalPipe],
  templateUrl: './evento-detalle.html',
})
export class EventoDetalle {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(EventosApiService);
  private readonly reservasApi = inject(ReservasApiService);
  private readonly noti = inject(NotificacionService);
  protected readonly session = inject(SessionService);

  protected readonly etiquetaTipo = etiquetaTipo;
  protected readonly etiquetaEstadoEvento = etiquetaEstadoEvento;
  protected readonly badgeEstadoEvento = badgeEstadoEvento;
  protected readonly etiquetaEstadoReserva = etiquetaEstadoReserva;
  protected readonly badgeEstadoReserva = badgeEstadoReserva;

  protected readonly evento = signal<Evento | null>(null);
  protected readonly cargando = signal(true);
  protected readonly reservas = signal<Reserva[]>([]);
  protected readonly accionandoId = signal<string | null>(null);
  protected readonly id = this.route.snapshot.paramMap.get('id') ?? '';

  constructor() {
    this.api.obtener(this.id).subscribe({
      next: (e) => {
        this.evento.set(e);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false),
    });

    if (this.session.esAdmin()) {
      this.cargarReservas();
    }
  }

  protected confirmar(r: Reserva): void {
    this.accionandoId.set(r.id);
    this.reservasApi.confirmar(r.id).subscribe({
      next: (actualizada) => {
        this.reemplazar(actualizada);
        this.accionandoId.set(null);
        this.noti.exito(`Pago confirmado. Código ${actualizada.codigo}.`);
        this.refrescarEvento();
      },
      error: () => this.accionandoId.set(null),
    });
  }

  protected cancelar(r: Reserva): void {
    this.accionandoId.set(r.id);
    this.reservasApi.cancelar(r.id).subscribe({
      next: (actualizada) => {
        this.reemplazar(actualizada);
        this.accionandoId.set(null);
        this.noti.exito('Reserva cancelada.');
        this.refrescarEvento();
      },
      error: () => this.accionandoId.set(null),
    });
  }

  protected cancelable(r: Reserva): boolean {
    return r.estado === 'pendiente_pago' || r.estado === 'confirmada';
  }

  private cargarReservas(): void {
    this.api.reservasDeEvento(this.id).subscribe((rs) => this.reservas.set(rs));
  }

  private reemplazar(r: Reserva): void {
    this.reservas.update((lista) => lista.map((x) => (x.id === r.id ? r : x)));
  }

  private refrescarEvento(): void {
    this.api.obtener(this.id).subscribe((e) => this.evento.set(e));
  }
}
