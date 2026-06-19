import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { EventosApiService } from '../../core/api/eventos-api.service';
import { VenuesApiService } from '../../core/api/venues-api.service';
import { SessionService } from '../../core/auth/session.service';
import {
  ESTADOS_EVENTO,
  EstadoEvento,
  EventoFiltro,
  EventoResumen,
  TIPOS_EVENTO,
  TipoEvento,
} from '../../core/models/evento.models';
import { Venue } from '../../core/models/venue.models';
import { badgeEstadoEvento, etiquetaEstadoEvento, etiquetaTipo } from '../../shared/format';

@Component({
  selector: 'app-eventos-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, DatePipe],
  templateUrl: './eventos-list.html',
})
export class EventosList {
  private readonly api = inject(EventosApiService);
  private readonly venuesApi = inject(VenuesApiService);
  protected readonly session = inject(SessionService);

  protected readonly tipos = TIPOS_EVENTO;
  protected readonly estados = ESTADOS_EVENTO;
  protected readonly etiquetaTipo = etiquetaTipo;
  protected readonly etiquetaEstadoEvento = etiquetaEstadoEvento;
  protected readonly badgeEstadoEvento = badgeEstadoEvento;

  protected readonly cargando = signal(false);
  protected readonly eventos = signal<EventoResumen[]>([]);
  protected readonly total = signal(0);
  protected readonly venues = signal<Venue[]>([]);
  protected readonly pagina = signal(1);
  private readonly tamano = 10;

  protected readonly totalPaginas = computed(() => Math.max(1, Math.ceil(this.total() / this.tamano)));

  protected readonly filtros = new FormGroup({
    titulo: new FormControl('', { nonNullable: true }),
    tipo: new FormControl<TipoEvento | ''>('', { nonNullable: true }),
    estado: new FormControl<EstadoEvento | ''>('', { nonNullable: true }),
    venueId: new FormControl<number | ''>('', { nonNullable: true }),
  });

  constructor() {
    this.venuesApi.listar().subscribe((v) => this.venues.set(v));
    this.buscar();
  }

  protected nombreVenue(id: number): string {
    return this.venues().find((v) => v.id === id)?.nombre ?? `#${id}`;
  }

  protected aplicar(): void {
    this.pagina.set(1);
    this.buscar();
  }

  protected limpiar(): void {
    this.filtros.reset({ titulo: '', tipo: '', estado: '', venueId: '' });
    this.aplicar();
  }

  protected irPagina(p: number): void {
    if (p >= 1 && p <= this.totalPaginas()) {
      this.pagina.set(p);
      this.buscar();
    }
  }

  private buscar(): void {
    const f = this.filtros.getRawValue();
    const filtro: EventoFiltro = {
      pagina: this.pagina(),
      tamanoPagina: this.tamano,
      titulo: f.titulo || undefined,
      tipo: f.tipo || undefined,
      estado: f.estado || undefined,
      venueId: f.venueId ? Number(f.venueId) : undefined,
    };

    this.cargando.set(true);
    this.api.listar(filtro).subscribe({
      next: (r) => {
        this.eventos.set(r.items);
        this.total.set(r.total);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false),
    });
  }
}
