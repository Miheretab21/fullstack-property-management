export interface Lease {
  id: string;
  unitId: string;
  unitNumber: string;
  propertyId: string;
  propertyName: string;
  tenantId: string;
  tenantName: string;
  tenantEmail: string;
  startDate: string;
  endDate: string;
  monthlyRent: number;
  securityDeposit: number;
  isActive: boolean;
  createdAtUtc: string;
}

export interface CreateLeaseRequest {
  unitId: string;
  tenantId: string;
  startDate: string;
  endDate: string;
  monthlyRent: number;
  securityDeposit: number;
  recordInitialPayment?: boolean;
  paymentMethod?: string;
  checkNumber?: string;
  bankName?: string;
  initialPaymentAmount?: number;
  isPaymentCleared?: boolean;
}
