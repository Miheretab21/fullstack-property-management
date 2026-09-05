import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ServiceResult } from '../models/auth.models';
import { UserItem, UpdateRoleRequest, CreateUserRequest, AdminUpdateUserRequest } from '../models/user.models';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/Users`;

  getAllUsers(): Observable<ServiceResult<UserItem[]>> {
    return this.http.get<ServiceResult<UserItem[]>>(this.baseUrl);
  }

  getUserById(id: string): Observable<ServiceResult<UserItem>> {
    return this.http.get<ServiceResult<UserItem>>(`${this.baseUrl}/${id}`);
  }

  createUser(request: CreateUserRequest): Observable<ServiceResult<UserItem>> {
    return this.http.post<ServiceResult<UserItem>>(this.baseUrl, request);
  }

  adminUpdateUser(id: string, request: AdminUpdateUserRequest): Observable<ServiceResult<UserItem>> {
    return this.http.put<ServiceResult<UserItem>>(`${this.baseUrl}/${id}/admin`, request);
  }

  deleteUser(id: string): Observable<ServiceResult<any>> {
    return this.http.delete<ServiceResult<any>>(`${this.baseUrl}/${id}`);
  }

  updateRole(id: string, role: string): Observable<ServiceResult<UserItem>> {
    return this.http.put<ServiceResult<UserItem>>(`${this.baseUrl}/${id}/role`, { role });
  }

  toggleStatus(id: string): Observable<ServiceResult<any>> {
    return this.http.patch<ServiceResult<any>>(`${this.baseUrl}/${id}/toggle-status`, {});
  }
}
