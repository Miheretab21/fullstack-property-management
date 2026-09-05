import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ServiceResult } from '../models/auth.models';
import { CreateDirectPaymentRequest, CreateRentChargeRequest, RecordPaymentRequest, TenantLedgerSummary, Transaction, TransactionStatus } from '../models/financial.models';

@Injectable({
  providedIn: 'root'
})
export class FinancialService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/Financial`;

  getTransactions(leaseId?: string, tenantId?: string, status?: TransactionStatus): Observable<ServiceResult<Transaction[]>> {
    let params = new HttpParams();
    if (leaseId) params = params.set('leaseId', leaseId);
    if (tenantId) params = params.set('tenantId', tenantId);
    if (status) params = params.set('status', status);

    return this.http.get<ServiceResult<Transaction[]>>(`${this.baseUrl}/transactions`, { params });
  }

  getTransactionById(id: string): Observable<ServiceResult<Transaction>> {
    return this.http.get<ServiceResult<Transaction>>(`${this.baseUrl}/transactions/${id}`);
  }

  getTenantLedger(leaseId: string): Observable<ServiceResult<TenantLedgerSummary>> {
    return this.http.get<ServiceResult<TenantLedgerSummary>>(`${this.baseUrl}/ledger/${leaseId}`);
  }

  createRentCharge(request: CreateRentChargeRequest): Observable<ServiceResult<Transaction>> {
    return this.http.post<ServiceResult<Transaction>>(`${this.baseUrl}/charges`, request);
  }

  recordPayment(request: RecordPaymentRequest): Observable<ServiceResult<Transaction>> {
    return this.http.post<ServiceResult<Transaction>>(`${this.baseUrl}/payments`, request);
  }

  createDirectPayment(request: CreateDirectPaymentRequest): Observable<ServiceResult<Transaction>> {
    return this.http.post<ServiceResult<Transaction>>(`${this.baseUrl}/direct-payments`, request);
  }
}
