import { UserRole } from './auth.models';

export interface UserItem {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  roles: string[];
  isActive: boolean;
  createdAtUtc: string;
}

export interface UpdateRoleRequest {
  role: UserRole;
}

export interface CreateUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  password: string;
  role: UserRole;
}

export interface AdminUpdateUserRequest {
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  role: UserRole;
}
