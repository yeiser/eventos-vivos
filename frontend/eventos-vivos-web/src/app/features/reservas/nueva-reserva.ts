import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { EventosApiService } from '../../core/api/eventos-api.service';
import { EventoResumen } from '../../core/models/evento.models';
import { CrearReservaRequest } from '../../core/models/reserva.models';
import { NotificacionService } from '../../core/notificaciones/notificacion.service';
import { etiquetaTipo, limiteEntradas } from '../../shared/format';

@Component({
  selector: 'app-nueva-reserva',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './nueva-reserva.html',
})
export class NuevaReserva {
  private readonly api = inject(EventosApiService);
  private readonly router = inject(Router);
  private readonly noti = inject(NotificacionService);

  protected readonly etiquetaTipo = etiquetaTipo;
  protected readonly eventos = signal<EventoResumen[]>([]);
  protected readonly eventoSel = signal<EventoResumen | null>(null);
  protected readonly guardando = signal(false);

  protected readonly limite = computed(() => {
    const e = this.eventoSel();
    return e ? limiteEntradas(e.fechaInicio, e.precio, e.entradasDisponibles) : 0;
  });

  protected readonly form = new FormGroup({
    eventoId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    cantidad: new FormControl<number | null>(1, { validators: [Validators.required, Validators.min(1)] }),
    nombreComprador: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    emailComprador: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
  });

  constructor() {
    this.api.listar({ estado: 'activo', tamanoPagina: 100 }).subscribe((r) => this.eventos.set(r.items));
  }

  protected seleccionarEvento(): void {
    const id = this.form.controls.eventoId.value;
    this.eventoSel.set(this.eventos().find((e) => e.id === id) ?? null);
  }

  protected invalido(campo: string): boolean {
    const c = this.form.get(campo);
    return !!c && c.invalid && c.touched;
  }

  protected excedeLimite(): boolean {
    return Number(this.form.controls.cantidad.value ?? 0) > this.limite();
  }

  protected reservar(): void {
    if (this.form.invalid || this.limite() === 0 || this.excedeLimite()) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    const request: CrearReservaRequest = {
      cantidad: Number(v.cantidad),
      nombreComprador: v.nombreComprador,
      emailComprador: v.emailComprador,
    };

    this.guardando.set(true);
    this.api.reservar(v.eventoId, request).subscribe({
      next: (r) => {
        this.noti.exito('Reserva creada (pendiente de pago).');
        this.router.navigate(['/reservas', r.id]);
      },
      error: () => this.guardando.set(false),
    });
  }
}
