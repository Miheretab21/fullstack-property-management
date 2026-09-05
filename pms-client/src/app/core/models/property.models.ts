export type UnitStatus = 'Vacant' | 'Occupied' | 'Maintenance' | 'UnderFinishing';

export interface Property {
  id: string;
  name: string;
  address: string;
  subCity: string;
  city: string;
  ownerId: string;
  assignedManagerId?: string;
  assignedManagerName?: string;
  totalFloors: number;
  totalSquareMeters: number;
  constructionStatus: string;
  finishingNotes?: string;
  totalUnits: number;
  occupiedUnits: number;
  vacantUnits: number;
  createdAtUtc: string;
}

export interface Unit {
  id: string;
  propertyId: string;
  propertyName?: string;
  subCity?: string;
  unitNumber: string;
  floorNumber: number;
  squareMeters: number;
  bedrooms: number;
  bathrooms: number;
  rentAmount: number;
  status: UnitStatus;
  finishingNotes?: string;
  createdAtUtc: string;
}

export interface PropertyDetail extends Property {
  units: Unit[];
}

export interface CreatePropertyRequest {
  name: string;
  address: string;
  subCity?: string;
  city: string;
  ownerId?: string;
  assignedManagerId?: string;
  totalFloors?: number;
  totalSquareMeters?: number;
  constructionStatus?: string;
  finishingNotes?: string;
}

export interface CreateUnitRequest {
  propertyId: string;
  unitNumber: string;
  floorNumber?: number;
  squareMeters?: number;
  bedrooms: number;
  bathrooms: number;
  rentAmount: number;
  status: UnitStatus;
  finishingNotes?: string;
}
