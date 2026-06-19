import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { NotificacionService } from '../../core/notificaciones/notificacion.service';

@Component({
  selector: 'app-notificaciones',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './notificaciones.html',
  styleUrl: './notificaciones.scss',
})
export class Notificaciones {
  protected readonly noti = inject(NotificacionService);
}
