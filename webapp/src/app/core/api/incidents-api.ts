import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Incident, IncidentStatus } from './models';

@Injectable({ providedIn: 'root' })
export class IncidentsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/incidents';

  list(status?: IncidentStatus): Observable<Incident[]> {
    const params: Record<string, string> = status ? { status: status.toLowerCase() } : {};
    return this.http.get<Incident[]>(this.baseUrl, { params });
  }

  resolve(id: string): Observable<Incident> {
    return this.http.post<Incident>(`${this.baseUrl}/${id}/resolve`, null);
  }
}
