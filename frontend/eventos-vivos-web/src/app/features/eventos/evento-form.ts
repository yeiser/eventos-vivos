import { KeyValuePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { EventosApiService } from '../../core/api/eventos-api.service';
import { VenuesApiService } from '../../core/api/venues-api.service';
import { CrearEventoRequest, TIPOS_EVENTO, TipoEvento } from '../../core/models/evento.models';
import { ProblemDetails } from '../../core/models/common.models';
import { Venue } from '../../core/models/venue.models';
import { NotificacionService } from '../../core/notificaciones/notificacion.service';
import { aIsoConOffset, etiquetaTipo } from '../../shared/format';

@Component({
  selector: 'app-evento-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, KeyValuePipe],
  templateUrl: './evento-form.html',
})
export class EventoForm {
  private readonly api = inject(EventosApiService);
  private readonly venuesApi = inject(VenuesApiService);
  private readonly router = inject(Router);
  private readonly noti = inject(NotificacionService);

  protected readonly tipos = TIPOS_EVENTO;
  protected readonly etiquetaTipo = etiquetaTipo;
  protected readonly venues = signal<Venue[]>([]);
  protected readonly guardando = signal(false);
  protected readonly erroresServidor = signal<Record<string, string[]>>({});

  protected readonly form = new FormGroup({
    titulo: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(5), Validators.maxLength(100)] }),
    descripcion: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(10), Validators.maxLength(500)] }),
    venueId: new FormControl<number | ''>('', { nonNullable: true, validators: [Validators.required] }),
    capacidadMaxima: new FormControl<number | null>(null, { validators: [Validators.required, Validators.min(1)] }),
    fechaInicio: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    fechaFin: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    precio: new FormControl<number | null>(null, { validators: [Validators.required, Validators.min(0.01)] }),
    tipo: new FormControl<TipoEvento | ''>('', { nonNullable: true, validators: [Validators.required] }),
  });

  constructor() {
    this.venuesApi.listar().subscribe((v) => this.venues.set(v));
  }

  protected invalido(campo: string): boolean {
    const c = this.form.get(campo);
    return !!c && c.invalid && c.touched;
  }

  protected crear(): void {
    this.erroresServidor.set({});
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    const request: CrearEventoRequest = {
      titulo: v.titulo,
      descripcion: v.descripcion,
      venueId: Number(v.venueId),
      capacidadMaxima: Number(v.capacidadMaxima),
      precio: Number(v.precio),
      tipo: v.tipo as TipoEvento,
      fechaInicio: aIsoConOffset(v.fechaInicio),
      fechaFin: aIsoConOffset(v.fechaFin),
    };

    this.guardando.set(true);
    this.api.crear(request).subscribe({
      next: (e) => {
        this.noti.exito('Evento creado correctamente.');
        this.router.navigate(['/eventos', e.id]);
      },
      error: (err: HttpErrorResponse) => {
        this.guardando.set(false);
        const problema = err.error as ProblemDetails | undefined;
        if (problema?.errors) {
          this.erroresServidor.set(problema.errors);
        }
      },
    });
  }
}
