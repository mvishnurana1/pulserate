import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Check, CheckInput, CheckResult } from './models';

/** Typed wrapper around /api/checks. Paths are relative, so the dev server proxy forwards them to the API. */
@Injectable({ providedIn: 'root' })
export class ChecksApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/checks';

  list(): Observable<Check[]> {
    return this.http.get<Check[]>(this.baseUrl);
  }

  get(id: string): Observable<Check> {
    return this.http.get<Check>(`${this.baseUrl}/${id}`);
  }

  results(id: string, take = 50): Observable<CheckResult[]> {
    return this.http.get<CheckResult[]>(`${this.baseUrl}/${id}/results`, { params: { take } });
  }

  create(input: CheckInput): Observable<Check> {
    return this.http.post<Check>(this.baseUrl, input);
  }

  update(id: string, input: CheckInput): Observable<Check> {
    return this.http.put<Check>(`${this.baseUrl}/${id}`, input);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  pause(id: string): Observable<Check> {
    return this.http.post<Check>(`${this.baseUrl}/${id}/pause`, null);
  }

  resume(id: string): Observable<Check> {
    return this.http.post<Check>(`${this.baseUrl}/${id}/resume`, null);
  }
}
