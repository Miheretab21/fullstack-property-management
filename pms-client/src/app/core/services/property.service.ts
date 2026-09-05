import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ServiceResult } from '../models/auth.models';
import { CreatePropertyRequest, CreateUnitRequest, Property, PropertyDetail, Unit, UnitStatus } from '../models/property.models';

@Injectable({
  providedIn: 'root'
})
export class PropertyService {
  private http = inject(HttpClient);
  private propertiesUrl = `${environment.apiUrl}/Properties`;
  private unitsUrl = `${environment.apiUrl}/Units`;

  // Properties
  getProperties(): Observable<ServiceResult<Property[]>> {
    return this.http.get<ServiceResult<Property[]>>(this.propertiesUrl);
  }

  getPropertyById(id: string): Observable<ServiceResult<PropertyDetail>> {
    return this.http.get<ServiceResult<PropertyDetail>>(`${this.propertiesUrl}/${id}`);
  }

  createProperty(request: CreatePropertyRequest): Observable<ServiceResult<Property>> {
    return this.http.post<ServiceResult<Property>>(this.propertiesUrl, request);
  }

  updateProperty(id: string, request: CreatePropertyRequest): Observable<ServiceResult<Property>> {
    return this.http.put<ServiceResult<Property>>(`${this.propertiesUrl}/${id}`, request);
  }

  deleteProperty(id: string): Observable<ServiceResult> {
    return this.http.delete<ServiceResult>(`${this.propertiesUrl}/${id}`);
  }

  // Units
  getUnits(propertyId?: string, status?: UnitStatus): Observable<ServiceResult<Unit[]>> {
    let params = new HttpParams();
    if (propertyId) params = params.set('propertyId', propertyId);
    if (status) params = params.set('status', status);

    return this.http.get<ServiceResult<Unit[]>>(this.unitsUrl, { params });
  }

  getUnitById(id: string): Observable<ServiceResult<Unit>> {
    return this.http.get<ServiceResult<Unit>>(`${this.unitsUrl}/${id}`);
  }

  createUnit(request: CreateUnitRequest): Observable<ServiceResult<Unit>> {
    return this.http.post<ServiceResult<Unit>>(this.unitsUrl, request);
  }

  updateUnit(id: string, request: Partial<CreateUnitRequest>): Observable<ServiceResult<Unit>> {
    return this.http.put<ServiceResult<Unit>>(`${this.unitsUrl}/${id}`, request);
  }

  updateUnitStatus(id: string, status: UnitStatus): Observable<ServiceResult<Unit>> {
    return this.http.patch<ServiceResult<Unit>>(`${this.unitsUrl}/${id}/status`, { status });
  }

  deleteUnit(id: string): Observable<ServiceResult> {
    return this.http.delete<ServiceResult>(`${this.unitsUrl}/${id}`);
  }
}
