import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { AuditoriaApiService } from '../../core/api/auditoria-api.service';
import { AccionAuditoria, AuditLog, AuditoriaFiltro } from '../../core/models/auditoria.models';
import { badgeAccion, etiquetaAccion, formatearJson } from '../../shared/format';

@Component({
  selector: 'app-auditoria',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, DatePipe],
  templateUrl: './auditoria.html',
})
export class Auditoria {
  private readonly api = inject(AuditoriaApiService);

  protected readonly etiquetaAccion = etiquetaAccion;
  protected readonly badgeAccion = badgeAccion;
  protected readonly formatearJson = formatearJson;

  protected readonly entidades = ['Evento', 'Reserva', 'Usuario'];
  protected readonly acciones: AccionAuditoria[] = ['crear', 'actualizar', 'eliminar'];

  protected readonly cargando = signal(false);
  protected readonly items = signal<AuditLog[]>([]);
  protected readonly total = signal(0);
  protected readonly pagina = signal(1);
  protected readonly expandido = signal<string | null>(null);
  private readonly tamano = 15;

  protected readonly totalPaginas = computed(() => Math.max(1, Math.ceil(this.total() / this.tamano)));

  protected readonly filtros = new FormGroup({
    entidad: new FormControl('', { nonNullable: true }),
    accion: new FormControl<AccionAuditoria | ''>('', { nonNullable: true }),
    usuario: new FormControl('', { nonNullable: true }),
  });

  constructor() {
    this.buscar();
  }

  protected aplicar(): void {
    this.pagina.set(1);
    this.buscar();
  }

  protected limpiar(): void {
    this.filtros.reset({ entidad: '', accion: '', usuario: '' });
    this.aplicar();
  }

  protected irPagina(p: number): void {
    if (p >= 1 && p <= this.totalPaginas()) {
      this.pagina.set(p);
      this.buscar();
    }
  }

  protected alternarDetalle(id: string): void {
    this.expandido.update((actual) => (actual === id ? null : id));
  }

  private buscar(): void {
    const f = this.filtros.getRawValue();
    const filtro: AuditoriaFiltro = {
      pagina: this.pagina(),
      tamanoPagina: this.tamano,
      entidad: f.entidad || undefined,
      accion: f.accion || undefined,
      usuario: f.usuario || undefined,
    };

    this.cargando.set(true);
    this.api.listar(filtro).subscribe({
      next: (r) => {
        this.items.set(r.items);
        this.total.set(r.total);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false),
    });
  }
}
