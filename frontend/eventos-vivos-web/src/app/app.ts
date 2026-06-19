import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Notificaciones } from './shared/notificaciones/notificaciones';

@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, Notificaciones],
  templateUrl: './app.html',
})
export class App {}
