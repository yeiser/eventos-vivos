import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Venue } from '../models/venue.models';

@Injectable({ providedIn: 'root' })
export class VenuesApiService {
  private readonly http = inject(HttpClient);

  listar(): Observable<Venue[]> {
    return this.http.get<Venue[]>(`${environment.apiBaseUrl}/venues`);
  }
}
