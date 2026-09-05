import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ServiceResult } from '../models/auth.models';
import { CreateMaintenanceRequest, MaintenancePriority, MaintenanceRequest, MaintenanceStatus, UpdateMaintenanceStatusRequest } from '../models/maintenance.models';

@Injectable({
  providedIn: 'root'
})
export class MaintenanceService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/Maintenance`;

  getRequests(tenantId?: string, unitId?: string, status?: MaintenanceStatus, priority?: MaintenancePriority): Observable<ServiceResult<MaintenanceRequest[]>> {
    let params = new HttpParams();
    if (tenantId) params = params.set('tenantId', tenantId);
    if (unitId) params = params.set('unitId', unitId);
    if (status) params = params.set('status', status);
    if (priority) params = params.set('priority', priority);

    return this.http.get<ServiceResult<MaintenanceRequest[]>>(this.baseUrl, { params });
  }

  getRequestById(id: string): Observable<ServiceResult<MaintenanceRequest>> {
    return this.http.get<ServiceResult<MaintenanceRequest>>(`${this.baseUrl}/${id}`);
  }

  createRequest(request: CreateMaintenanceRequest): Observable<ServiceResult<MaintenanceRequest>> {
    return this.http.post<ServiceResult<MaintenanceRequest>>(this.baseUrl, request);
  }

  updateRequestStatus(id: string, request: UpdateMaintenanceStatusRequest): Observable<ServiceResult<MaintenanceRequest>> {
    return this.http.patch<ServiceResult<MaintenanceRequest>>(`${this.baseUrl}/${id}/status`, request);
  }

  deleteRequest(id: string): Observable<ServiceResult> {
    return this.http.delete<ServiceResult>(`${this.baseUrl}/${id}`);
  }
}
