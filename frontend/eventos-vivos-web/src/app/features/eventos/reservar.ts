import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { EventosApiService } from '../../core/api/eventos-api.service';
import { Evento } from '../../core/models/evento.models';
import { CrearReservaRequest } from '../../core/models/reserva.models';
import { NotificacionService } from '../../core/notificaciones/notificacion.service';
import { limiteEntradas } from '../../shared/format';

@Component({
  selector: 'app-reservar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './reservar.html',
})
export class Reservar {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(EventosApiService);
  private readonly noti = inject(NotificacionService);

  protected readonly id = this.route.snapshot.paramMap.get('id') ?? '';
  protected readonly evento = signal<Evento | null>(null);
  protected readonly guardando = signal(false);

  /** Límite efectivo de entradas por transacción (espejo de la composición A-02 del backend). */
  protected readonly limite = computed(() => {
    const e = this.evento();
    return e ? limiteEntradas(e.fechaInicio, e.precio, e.entradasDisponibles) : 0;
  });

  protected readonly form = new FormGroup({
    cantidad: new FormControl<number | null>(1, { validators: [Validators.required, Validators.min(1)] }),
    nombreComprador: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    emailComprador: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
  });

  constructor() {
    this.api.obtener(this.id).subscribe((e) => this.evento.set(e));
  }

  protected invalido(campo: string): boolean {
    const c = this.form.get(campo);
    return !!c && c.invalid && c.touched;
  }

  protected excedeLimite(): boolean {
    const cantidad = Number(this.form.controls.cantidad.value ?? 0);
    return cantidad > this.limite();
  }

  protected reservar(): void {
    if (this.form.invalid || this.excedeLimite() || this.limite() === 0) {
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
    this.api.reservar(this.id, request).subscribe({
      next: (r) => {
        this.noti.exito('Reserva creada (pendiente de pago).');
        this.router.navigate(['/reservas', r.id]);
      },
      error: () => this.guardando.set(false),
    });
  }
}
