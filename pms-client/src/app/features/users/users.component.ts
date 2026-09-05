import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { UserService } from '../../core/services/user.service';
import { UserItem, CreateUserRequest, AdminUpdateUserRequest } from '../../core/models/user.models';
import { UserRole } from '../../core/models/auth.models';
import { ModalComponent } from '../../shared/components/modal.component';
import { NotificationService } from '../../core/services/notification.service';
import { AuthStore } from '../../store/auth.store';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, ModalComponent],
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss'
})
export class UsersComponent implements OnInit {
  private readonly userService = inject(UserService);
  private readonly notificationService = inject(NotificationService);
  private readonly fb = inject(FormBuilder);
  readonly authStore = inject(AuthStore);

  users = signal<UserItem[]>([]);
  isLoading = signal<boolean>(false);
  isSubmitting = signal<boolean>(false);
  searchTerm = signal<string>('');
  selectedRoleFilter = signal<string>('ALL');
  selectedStatusFilter = signal<string>('ALL');

  isCreateModalOpen = signal<boolean>(false);
  isEditModalOpen = signal<boolean>(false);
  isDeleteModalOpen = signal<boolean>(false);
  selectedUser = signal<UserItem | null>(null);

  private readonly ethiopianPhoneRegex = /^(\+251|0)[79]\d{8}$/;

  createForm = this.fb.group({
    firstName: ['', [Validators.required, Validators.maxLength(50)]],
    lastName: ['', [Validators.required, Validators.maxLength(50)]],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: ['', [Validators.pattern(this.ethiopianPhoneRegex)]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    role: ['Tenant' as UserRole, [Validators.required]]
  });

  editForm = this.fb.group({
    firstName: ['', [Validators.required, Validators.maxLength(50)]],
    lastName: ['', [Validators.required, Validators.maxLength(50)]],
    phoneNumber: ['', [Validators.pattern(this.ethiopianPhoneRegex)]],
    role: ['Tenant' as UserRole, [Validators.required]]
  });

  activeCount = computed(() => this.users().filter(u => u.isActive).length);
  managerCount = computed(() => this.users().filter(u => u.roles.includes('PropertyManager')).length);
  tenantCount = computed(() => this.users().filter(u => u.roles.includes('Tenant')).length);

  filteredUsers = computed(() => {
    const term = this.searchTerm().toLowerCase().trim();
    const roleFilter = this.selectedRoleFilter();
    const statusFilter = this.selectedStatusFilter();

    return this.users().filter(u => {
      const matchesSearch = !term ||
        u.firstName?.toLowerCase().includes(term) ||
        u.lastName?.toLowerCase().includes(term) ||
        u.email?.toLowerCase().includes(term) ||
        u.phoneNumber?.toLowerCase().includes(term);

      const matchesRole = roleFilter === 'ALL' || u.roles.includes(roleFilter);
      const matchesStatus = statusFilter === 'ALL' ||
        (statusFilter === 'ACTIVE' && u.isActive) ||
        (statusFilter === 'INACTIVE' && !u.isActive);

      return matchesSearch && matchesRole && matchesStatus;
    });
  });

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.isLoading.set(true);
    this.userService.getAllUsers().subscribe({
      next: (res) => {
        if (res.succeeded && res.data) {
          this.users.set(res.data);
        }
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  onSearchInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.searchTerm.set(input.value);
  }

  onRoleFilterChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.selectedRoleFilter.set(select.value);
  }

  onStatusFilterChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.selectedStatusFilter.set(select.value);
  }

  getRoleDisplayName(roles: string[]): string {
    if (roles.includes('Admin')) return 'Admin';
    if (roles.includes('PropertyManager')) return 'Property Manager';
    if (roles.includes('Tenant')) return 'Tenant';
    return roles[0] || 'User';
  }

  getRoleBadgeClass(roles: string[]): string {
    if (roles.includes('Admin')) return 'bg-purple-subtle text-purple border border-purple-subtle';
    if (roles.includes('PropertyManager')) return 'bg-primary-subtle text-primary border border-primary-subtle';
    if (roles.includes('Tenant')) return 'bg-success-subtle text-success border border-success-subtle';
    return 'bg-secondary-subtle text-secondary';
  }

  getRoleAvatarClass(roles: string[]): string {
    if (roles.includes('Admin')) return 'avatar-admin';
    if (roles.includes('PropertyManager')) return 'avatar-manager';
    return 'avatar-tenant';
  }

  isSelf(user: UserItem): boolean {
    return this.authStore.currentUser()?.id === user.id;
  }

  // Create User
  openCreateModal(): void {
    this.createForm.reset({
      firstName: '',
      lastName: '',
      email: '',
      phoneNumber: '',
      password: '',
      role: 'Tenant'
    });
    this.isCreateModalOpen.set(true);
  }

  closeCreateModal(): void {
    this.isCreateModalOpen.set(false);
  }

  submitCreateUser(): void {
    if (this.createForm.invalid) return;

    this.isSubmitting.set(true);
    const formVal = this.createForm.value;
    const req: CreateUserRequest = {
      firstName: formVal.firstName!.trim(),
      lastName: formVal.lastName!.trim(),
      email: formVal.email!.trim(),
      phoneNumber: formVal.phoneNumber ? formVal.phoneNumber.trim() : undefined,
      password: formVal.password!,
      role: formVal.role as UserRole
    };

    this.userService.createUser(req).subscribe({
      next: (res) => {
        this.isSubmitting.set(false);
        if (res.succeeded) {
          this.notificationService.success(`User ${req.firstName} ${req.lastName} created successfully.`);
          this.closeCreateModal();
          this.loadUsers();
        } else {
          this.notificationService.error(res.message || 'Failed to create user.');
        }
      },
      error: () => {
        this.isSubmitting.set(false);
      }
    });
  }

  // Edit User
  openEditModal(user: UserItem): void {
    this.selectedUser.set(user);
    const role: UserRole = user.roles.includes('Admin') ? 'Admin'
      : user.roles.includes('PropertyManager') ? 'PropertyManager'
      : 'Tenant';

    this.editForm.patchValue({
      firstName: user.firstName,
      lastName: user.lastName,
      phoneNumber: user.phoneNumber || '',
      role: role
    });
    this.isEditModalOpen.set(true);
  }

  closeEditModal(): void {
    this.isEditModalOpen.set(false);
    this.selectedUser.set(null);
  }

  submitEditUser(): void {
    if (this.editForm.invalid || !this.selectedUser()) return;

    this.isSubmitting.set(true);
    const user = this.selectedUser()!;
    const formVal = this.editForm.value;
    const req: AdminUpdateUserRequest = {
      firstName: formVal.firstName!.trim(),
      lastName: formVal.lastName!.trim(),
      phoneNumber: formVal.phoneNumber ? formVal.phoneNumber.trim() : undefined,
      role: formVal.role as UserRole
    };

    this.userService.adminUpdateUser(user.id, req).subscribe({
      next: (res) => {
        this.isSubmitting.set(false);
        if (res.succeeded) {
          this.notificationService.success(`User ${req.firstName} updated successfully.`);
          this.closeEditModal();
          this.loadUsers();
        } else {
          this.notificationService.error(res.message || 'Failed to update user.');
        }
      },
      error: () => {
        this.isSubmitting.set(false);
      }
    });
  }

  // Delete User
  openDeleteModal(user: UserItem): void {
    this.selectedUser.set(user);
    this.isDeleteModalOpen.set(true);
  }

  closeDeleteModal(): void {
    this.isDeleteModalOpen.set(false);
    this.selectedUser.set(null);
  }

  confirmDeleteUser(): void {
    if (!this.selectedUser()) return;

    const user = this.selectedUser()!;
    this.isSubmitting.set(true);

    this.userService.deleteUser(user.id).subscribe({
      next: (res) => {
        this.isSubmitting.set(false);
        if (res.succeeded) {
          this.notificationService.success(`User ${user.firstName} ${user.lastName} has been deleted.`);
          this.closeDeleteModal();
          this.loadUsers();
        } else {
          this.notificationService.error(res.message || 'Failed to delete user.');
        }
      },
      error: () => {
        this.isSubmitting.set(false);
      }
    });
  }

  // Toggle Status (Disable/Enable)
  toggleStatus(user: UserItem): void {
    this.userService.toggleStatus(user.id).subscribe({
      next: (res) => {
        if (res.succeeded) {
          const actionText = user.isActive ? 'deactivated' : 'activated';
          this.notificationService.success(`User ${user.firstName} has been ${actionText}.`);
          this.loadUsers();
        } else {
          this.notificationService.error(res.message || 'Failed to update user status.');
        }
      }
    });
  }
}
