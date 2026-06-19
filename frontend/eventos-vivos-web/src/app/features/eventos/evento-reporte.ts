import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { NgApexchartsModule } from 'ng-apexcharts';
import { EventosApiService } from '../../core/api/eventos-api.service';
import { ReporteOcupacion } from '../../core/models/evento.models';
import { badgeEstadoEvento, etiquetaEstadoEvento } from '../../shared/format';

@Component({
  selector: 'app-evento-reporte',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DecimalPipe, NgApexchartsModule],
  templateUrl: './evento-reporte.html',
})
export class EventoReporte {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(EventosApiService);

  protected readonly etiquetaEstadoEvento = etiquetaEstadoEvento;
  protected readonly badgeEstadoEvento = badgeEstadoEvento;

  protected readonly reporte = signal<ReporteOcupacion | null>(null);
  protected readonly cargando = signal(true);
  protected readonly id = this.route.snapshot.paramMap.get('id') ?? '';

  protected readonly chart = computed(() => {
    const r = this.reporte();
    return {
      series: [Math.round(r?.porcentajeOcupacion ?? 0)],
      chart: { type: 'radialBar' as const, height: 300 },
      plotOptions: {
        radialBar: {
          hollow: { size: '60%' },
          dataLabels: {
            name: { offsetY: -10, fontSize: '14px' },
            value: { fontSize: '28px', fontWeight: 700, formatter: (v: number) => `${v}%` },
          },
        },
      },
      colors: ['#009ef7'],
      labels: ['Ocupación'],
    };
  });

  constructor() {
    this.api.reporte(this.id).subscribe({
      next: (r) => {
        this.reporte.set(r);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false),
    });
  }
}
