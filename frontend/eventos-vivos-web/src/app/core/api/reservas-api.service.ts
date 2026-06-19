import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/common.models';
import { Reserva, ReservaFiltro, ReservaResumen } from '../models/reserva.models';

@Injectable({ providedIn: 'root' })
export class ReservasApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/reservas`;

  buscar(filtro: ReservaFiltro): Observable<PagedResult<ReservaResumen>> {
    let params = new HttpParams();
    for (const [clave, valor] of Object.entries(filtro)) {
      if (valor !== undefined && valor !== null && valor !== '') {
        params = params.set(clave, String(valor));
      }
    }
    return this.http.get<PagedResult<ReservaResumen>>(this.base, { params });
  }

  obtener(id: string): Observable<Reserva> {
    return this.http.get<Reserva>(`${this.base}/${id}`);
  }

  confirmar(id: string): Observable<Reserva> {
    return this.http.post<Reserva>(`${this.base}/${id}/confirmacion`, {});
  }

  cancelar(id: string): Observable<Reserva> {
    return this.http.post<Reserva>(`${this.base}/${id}/cancelacion`, {});
  }
}
