import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators, FormsModule } from '@angular/forms';
import { LeaseService } from '../../core/services/lease.service';
import { Lease, CreateLeaseRequest } from '../../core/models/lease.models';
import { FinancialService } from '../../core/services/financial.service';
import { NotificationService } from '../../core/services/notification.service';
import { AuthStore } from '../../store/auth.store';
import { PropertyService } from '../../core/services/property.service';
import { UserService } from '../../core/services/user.service';
import { Unit } from '../../core/models/property.models';
import { UserItem } from '../../core/models/user.models';
import { ModalComponent } from '../../shared/components/modal.component';

@Component({
  selector: 'app-leases',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, ModalComponent],
  templateUrl: './leases.component.html',
  styleUrl: './leases.component.scss'
})
export class LeasesComponent implements OnInit {
  private leaseService = inject(LeaseService);
  private financialService = inject(FinancialService);
  private propertyService = inject(PropertyService);
  private userService = inject(UserService);
  private notificationService = inject(NotificationService);
  public authStore = inject(AuthStore);
  private fb = inject(FormBuilder);

  leases = signal<Lease[]>([]);
  availableUnits = signal<Unit[]>([]);
  availableTenants = signal<UserItem[]>([]);

  isLoading = signal<boolean>(true);
  isSubmitting = signal<boolean>(false);

  // Search and filters
  searchTerm = signal<string>('');
  statusFilter = signal<'ALL' | 'ACTIVE' | 'TERMINATED'>('ALL');

  // Direct Create Lease Modal
  isCreateModalOpen = signal<boolean>(false);

  // Check Payment Modal State (for existing leases)
  isPayCheckModalOpen = signal<boolean>(false);
  selectedLeaseForPayment = signal<Lease | null>(null);
  isSubmittingPayment = signal<boolean>(false);

  // Forms
  leaseForm = this.fb.nonNullable.group({
    unitId: ['', [Validators.required]],
    tenantId: ['', [Validators.required]],
    startDate: ['', [Validators.required]],
    endDate: ['', [Validators.required]],
    monthlyRent: [0, [Validators.required, Validators.min(1)]],
    securityDeposit: [0, [Validators.required, Validators.min(0)]],
    recordInitialPayment: [true],
    paymentMethod: ['Check'],
    checkNumber: [''],
    bankName: ['Commercial Bank of Ethiopia'],
    initialPaymentAmount: [0, [Validators.min(0)]],
    isPaymentCleared: [true]
  });

  payCheckForm = this.fb.nonNullable.group({
    amount: [0, [Validators.required, Validators.min(1)]],
    paymentMethod: ['Check', [Validators.required]],
    checkNumber: ['', [Validators.required]],
    bankName: ['Commercial Bank of Ethiopia', [Validators.required]]
  });

  // Computed counters
  activeLeasesCount = computed(() => this.leases().filter(l => l.isActive).length);
  terminatedLeasesCount = computed(() => this.leases().filter(l => !l.isActive).length);

  filteredLeases = computed(() => {
    const term = this.searchTerm().toLowerCase().trim();
    const filter = this.statusFilter();

    return this.leases().filter(l => {
      const matchesSearch = !term ||
        l.tenantName?.toLowerCase().includes(term) ||
        l.tenantEmail?.toLowerCase().includes(term) ||
        l.propertyName?.toLowerCase().includes(term) ||
        l.unitNumber?.toLowerCase().includes(term);

      const matchesStatus = filter === 'ALL' ||
        (filter === 'ACTIVE' && l.isActive) ||
        (filter === 'TERMINATED' && !l.isActive);

      return matchesSearch && matchesStatus;
    });
  });

  ngOnInit() {
    this.loadLeases();
    this.loadUnitsAndTenants();
  }

  loadLeases() {
    this.isLoading.set(true);
    this.leaseService.getLeases().subscribe({
      next: (res) => {
        if (res.succeeded && res.data) {
          this.leases.set(res.data);
        }
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  loadUnitsAndTenants() {
    this.propertyService.getUnits().subscribe({
      next: (res) => {
        if (res.succeeded && res.data) {
          this.availableUnits.set(res.data);
        }
      }
    });

    if (this.authStore.isAdmin() || this.authStore.isPropertyManager()) {
      this.userService.getAllUsers().subscribe({
        next: (res) => {
          if (res.succeeded && res.data) {
            const tenantsOnly = res.data.filter(u => u.roles && u.roles.includes('Tenant'));
            this.availableTenants.set(tenantsOnly);
          }
        }
      });
    }
  }

  onSearchInput(event: Event) {
    const input = event.target as HTMLInputElement;
    this.searchTerm.set(input.value);
  }

  // ================= LEASE ACTIONS ================= //

  terminateLease(id: string) {
    if (confirm('Are you sure you want to terminate this lease agreement?')) {
      this.leaseService.terminateLease(id).subscribe({
        next: (res) => {
          if (res.succeeded) {
            this.notificationService.success('Lease terminated successfully.');
            this.loadLeases();
          } else {
            this.notificationService.error(res.message || 'Failed to terminate lease.');
          }
        }
      });
    }
  }

  deleteLease(lease: Lease) {
    const confirmed = confirm(
      `Are you sure you want to permanently delete the lease for Unit ${lease.unitNumber} (${lease.tenantName})?\n\nThis will remove all associated payment ledger transactions and free up the unit.`
    );
    if (confirmed) {
      this.leaseService.deleteLease(lease.id).subscribe({
        next: (res) => {
          if (res.succeeded) {
            this.notificationService.success('Lease agreement deleted successfully.');
            this.loadLeases();
          } else {
            this.notificationService.error(res.message || 'Failed to delete lease.');
          }
        },
        error: (err) => {
          this.notificationService.error(err.error?.message || 'Failed to delete lease.');
        }
      });
    }
  }

  openPayCheckModal(lease: Lease) {
    this.selectedLeaseForPayment.set(lease);
    this.payCheckForm.reset({
      amount: lease.monthlyRent,
      paymentMethod: 'Check',
      checkNumber: '',
      bankName: 'Commercial Bank of Ethiopia'
    });
    this.isPayCheckModalOpen.set(true);
  }

  submitDirectCheckPayment() {
    const lease = this.selectedLeaseForPayment();
    if (!lease || this.payCheckForm.invalid) return;

    this.isSubmittingPayment.set(true);
    const formVal = this.payCheckForm.getRawValue();

    this.financialService.createDirectPayment({
      leaseId: lease.id,
      amount: formVal.amount,
      paymentMethod: formVal.paymentMethod,
      checkNumber: formVal.checkNumber.trim(),
      bankName: formVal.bankName.trim(),
      isCleared: true,
      paymentDate: new Date().toISOString()
    }).subscribe({
      next: (res) => {
        this.isSubmittingPayment.set(false);
        if (res.succeeded) {
          this.notificationService.success(
            `Check payment of ETB ${formVal.amount.toLocaleString()} received for Unit ${lease.unitNumber}! Marked as Paid.`
          );
          this.isPayCheckModalOpen.set(false);
          this.loadLeases();
        } else {
          this.notificationService.error(res.message || 'Failed to record payment.');
        }
      },
      error: () => this.isSubmittingPayment.set(false)
    });
  }

  openCreateModal() {
    const today = new Date().toISOString().split('T')[0];
    const nextYear = new Date();
    nextYear.setFullYear(nextYear.getFullYear() + 1);
    const nextYearStr = nextYear.toISOString().split('T')[0];

    this.leaseForm.reset({
      unitId: '',
      tenantId: '',
      startDate: today,
      endDate: nextYearStr,
      monthlyRent: 0,
      securityDeposit: 0,
      recordInitialPayment: true,
      paymentMethod: 'Check',
      checkNumber: '',
      bankName: 'Commercial Bank of Ethiopia',
      initialPaymentAmount: 0,
      isPaymentCleared: true
    });
    this.isCreateModalOpen.set(true);
  }

  onUnitChange(event: Event) {
    const unitId = (event.target as HTMLSelectElement).value;
    const selected = this.availableUnits().find(u => u.id === unitId);
    if (selected) {
      this.leaseForm.patchValue({
        monthlyRent: selected.rentAmount,
        securityDeposit: selected.rentAmount,
        initialPaymentAmount: selected.rentAmount
      });
    }
  }

  submitLease() {
    if (this.leaseForm.valid) {
      this.isSubmitting.set(true);
      const val = this.leaseForm.getRawValue();
      const req: CreateLeaseRequest = {
        unitId: val.unitId,
        tenantId: val.tenantId,
        startDate: new Date(val.startDate).toISOString(),
        endDate: new Date(val.endDate).toISOString(),
        monthlyRent: Number(val.monthlyRent),
        securityDeposit: Number(val.securityDeposit),
        recordInitialPayment: val.recordInitialPayment,
        paymentMethod: val.paymentMethod,
        checkNumber: val.checkNumber?.trim() || undefined,
        bankName: val.bankName?.trim() || undefined,
        initialPaymentAmount: val.initialPaymentAmount ? Number(val.initialPaymentAmount) : Number(val.monthlyRent),
        isPaymentCleared: val.isPaymentCleared
      };

      this.leaseService.createLease(req).subscribe({
        next: (res) => {
          this.isSubmitting.set(false);
          if (res.succeeded) {
            this.notificationService.success('Lease agreement created successfully.');
            this.isCreateModalOpen.set(false);
            this.loadLeases();
          } else {
            this.notificationService.error(res.message || 'Failed to create lease.');
          }
        },
        error: () => this.isSubmitting.set(false)
      });
    }
  }
}
