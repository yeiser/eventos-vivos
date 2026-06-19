import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuditLog, AuditoriaFiltro } from '../models/auditoria.models';
import { PagedResult } from '../models/common.models';

@Injectable({ providedIn: 'root' })
export class AuditoriaApiService {
  private readonly http = inject(HttpClient);

  listar(filtro: AuditoriaFiltro): Observable<PagedResult<AuditLog>> {
    let params = new HttpParams();
    for (const [clave, valor] of Object.entries(filtro)) {
      if (valor !== undefined && valor !== null && valor !== '') {
        params = params.set(clave, String(valor));
      }
    }
    return this.http.get<PagedResult<AuditLog>>(`${environment.apiBaseUrl}/auditoria`, { params });
  }
}
