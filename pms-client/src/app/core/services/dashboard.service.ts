import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ServiceResult } from '../models/auth.models';
import { DashboardMetrics } from '../models/dashboard.models';

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/Dashboard`;

  getMetrics(): Observable<ServiceResult<DashboardMetrics>> {
    return this.http.get<ServiceResult<DashboardMetrics>>(`${this.baseUrl}/metrics`);
  }
}
