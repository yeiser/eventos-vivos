import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/common.models';
import {
  CrearEventoRequest,
  Evento,
  EventoFiltro,
  EventoResumen,
  ReporteOcupacion,
} from '../models/evento.models';
import { CrearReservaRequest, Reserva } from '../models/reserva.models';

@Injectable({ providedIn: 'root' })
export class EventosApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/eventos`;

  listar(filtro: EventoFiltro): Observable<PagedResult<EventoResumen>> {
    let params = new HttpParams();
    for (const [clave, valor] of Object.entries(filtro)) {
      if (valor !== undefined && valor !== null && valor !== '') {
        params = params.set(clave, String(valor));
      }
    }
    return this.http.get<PagedResult<EventoResumen>>(this.base, { params });
  }

  obtener(id: string): Observable<Evento> {
    return this.http.get<Evento>(`${this.base}/${id}`);
  }

  crear(request: CrearEventoRequest): Observable<Evento> {
    return this.http.post<Evento>(this.base, request);
  }

  reporte(id: string): Observable<ReporteOcupacion> {
    return this.http.get<ReporteOcupacion>(`${this.base}/${id}/reporte`);
  }

  reservar(eventoId: string, request: CrearReservaRequest): Observable<Reserva> {
    return this.http.post<Reserva>(`${this.base}/${eventoId}/reservas`, request);
  }

  reservasDeEvento(eventoId: string): Observable<Reserva[]> {
    return this.http.get<Reserva[]>(`${this.base}/${eventoId}/reservas`);
  }
}
