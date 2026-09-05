import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RegisterRequest, ServiceResult, UserProfile } from '../models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/Auth`;

  login(request: LoginRequest): Observable<ServiceResult<AuthResponse>> {
    return this.http.post<ServiceResult<AuthResponse>>(`${this.baseUrl}/login`, request);
  }

  register(request: RegisterRequest): Observable<ServiceResult<AuthResponse>> {
    return this.http.post<ServiceResult<AuthResponse>>(`${this.baseUrl}/register`, request);
  }

  getCurrentUser(): Observable<ServiceResult<UserProfile>> {
    return this.http.get<ServiceResult<UserProfile>>(`${this.baseUrl}/me`);
  }

  changePassword(currentPassword: string, newPassword: string): Observable<ServiceResult> {
    return this.http.post<ServiceResult>(`${this.baseUrl}/change-password`, { currentPassword, newPassword });
  }
}
