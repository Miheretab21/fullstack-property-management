export type MaintenancePriority = 'Low' | 'Medium' | 'High';
export type MaintenanceStatus = 'Open' | 'InProgress' | 'Closed';

export interface MaintenanceRequest {
  id: string;
  unitId: string;
  unitNumber: string;
  propertyName: string;
  tenantId: string;
  tenantName: string;
  title: string;
  description: string;
  priority: MaintenancePriority;
  status: MaintenanceStatus;
  photoUrl?: string;
  resolutionNotes?: string;
  assignedTechnician?: string;
  createdAtUtc: string;
  resolvedAtUtc?: string;
}

export interface CreateMaintenanceRequest {
  unitId: string;
  title: string;
  description: string;
  priority: MaintenancePriority;
  photoUrl?: string;
}

export interface UpdateMaintenanceStatusRequest {
  status: MaintenanceStatus;
  resolutionNotes?: string;
  assignedTechnician?: string;
}
