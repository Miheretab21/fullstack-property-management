import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ServiceResult } from '../models/auth.models';
import { CreateLeaseRequest, Lease } from '../models/lease.models';

@Injectable({
  providedIn: 'root'
})
export class LeaseService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/Leases`;

  getLeases(tenantId?: string, unitId?: string, activeOnly?: boolean): Observable<ServiceResult<Lease[]>> {
    let params = new HttpParams();
    if (tenantId) params = params.set('tenantId', tenantId);
    if (unitId) params = params.set('unitId', unitId);
    if (activeOnly !== undefined) params = params.set('activeOnly', activeOnly);

    return this.http.get<ServiceResult<Lease[]>>(this.baseUrl, { params });
  }

  getLeaseById(id: string): Observable<ServiceResult<Lease>> {
    return this.http.get<ServiceResult<Lease>>(`${this.baseUrl}/${id}`);
  }

  createLease(request: CreateLeaseRequest): Observable<ServiceResult<Lease>> {
    return this.http.post<ServiceResult<Lease>>(this.baseUrl, request);
  }

  updateLease(id: string, request: Partial<CreateLeaseRequest & { isActive: boolean }>): Observable<ServiceResult<Lease>> {
    return this.http.put<ServiceResult<Lease>>(`${this.baseUrl}/${id}`, request);
  }

  terminateLease(id: string): Observable<ServiceResult> {
    return this.http.post<ServiceResult>(`${this.baseUrl}/${id}/terminate`, {});
  }

  deleteLease(id: string): Observable<ServiceResult> {
    return this.http.delete<ServiceResult>(`${this.baseUrl}/${id}`);
  }
}
