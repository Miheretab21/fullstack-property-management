import { Transaction } from './financial.models';

export interface ExpiringLeaseSummary {
  leaseId: string;
  unitNumber: string;
  propertyName: string;
  tenantName: string;
  endDate: string;
  daysRemaining: number;
  monthlyRent: number;
}

export interface DashboardMetrics {
  totalProperties: number;
  totalUnits: number;
  occupiedUnits: number;
  vacantUnits: number;
  maintenanceUnits: number;
  finishingUnits: number;
  occupancyRate: number;
  totalMonthlyProjectedRevenue: number;
  currentMonthCollectedRevenue: number;
  currentMonthPendingRevenue: number;
  activeMaintenanceRequests: number;
  expiringLeasesCount: number;
  expiringLeases: ExpiringLeaseSummary[];
  recentTransactions: Transaction[];
}
