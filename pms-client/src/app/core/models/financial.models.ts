export type TransactionStatus = 'Paid' | 'Pending' | 'Overdue';

export interface Transaction {
  id: string;
  leaseId: string;
  unitId: string;
  unitNumber: string;
  propertyName: string;
  tenantId: string;
  tenantName: string;
  amount: number;
  paymentDate: string;
  paymentMethod: string;
  status: TransactionStatus;
  createdAtUtc: string;
}

export interface TenantLedgerSummary {
  leaseId: string;
  unitNumber: string;
  propertyName: string;
  monthlyRent: number;
  totalBilled: number;
  totalPaid: number;
  outstandingBalance: number;
  transactions: Transaction[];
}

export interface CreateRentChargeRequest {
  leaseId: string;
  amount: number;
  dueDate?: string;
}

export interface RecordPaymentRequest {
  transactionId: string;
  paymentMethod: string;
  checkNumber?: string;
  bankName?: string;
  paymentDate?: string;
}

export interface CreateDirectPaymentRequest {
  leaseId: string;
  amount: number;
  paymentMethod: string;
  checkNumber?: string;
  bankName?: string;
  isCleared?: boolean;
  paymentDate?: string;
}
