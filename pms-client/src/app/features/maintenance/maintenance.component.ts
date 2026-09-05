import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MaintenanceService } from '../../core/services/maintenance.service';
import { MaintenancePriority, MaintenanceRequest, MaintenanceStatus } from '../../core/models/maintenance.models';
import { ModalComponent } from '../../shared/components/modal.component';
import { NotificationService } from '../../core/services/notification.service';
import { AuthStore } from '../../store/auth.store';
import { PropertyService } from '../../core/services/property.service';
import { Unit } from '../../core/models/property.models';

@Component({
  selector: 'app-maintenance',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ModalComponent],
  templateUrl: './maintenance.component.html',
  styleUrl: './maintenance.component.scss'
})
export class MaintenanceComponent implements OnInit {
  private maintenanceService = inject(MaintenanceService);
  private propertyService = inject(PropertyService);
  private notificationService = inject(NotificationService);
  public authStore = inject(AuthStore);
  private fb = inject(FormBuilder);

  requests = signal<MaintenanceRequest[]>([]);
  availableUnits = signal<Unit[]>([]);
  isLoading = signal<boolean>(true);
  isCreateModalOpen = signal<boolean>(false);
  isStatusModalOpen = signal<boolean>(false);
  selectedRequest = signal<MaintenanceRequest | null>(null);

  ticketForm = this.fb.nonNullable.group({
    unitId: ['', [Validators.required]],
    title: ['', [Validators.required]],
    description: ['', [Validators.required]],
    priority: ['Medium' as MaintenancePriority, [Validators.required]]
  });

  statusForm = this.fb.nonNullable.group({
    status: ['InProgress' as MaintenanceStatus, [Validators.required]],
    assignedTechnician: [''],
    resolutionNotes: ['']
  });

  ngOnInit() {
    this.loadRequests();
    this.loadUnits();
  }

  loadRequests() {
    this.isLoading.set(true);
    this.maintenanceService.getRequests().subscribe({
      next: (res) => {
        if (res.succeeded && res.data) {
          this.requests.set(res.data);
        }
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  loadUnits() {
    this.propertyService.getUnits().subscribe({
      next: (res) => {
        if (res.succeeded && res.data) {
          this.availableUnits.set(res.data);
        }
      }
    });
  }

  getPriorityBadgeClass(priority: MaintenancePriority): string {
    switch (priority) {
      case 'High': return 'bg-danger-subtle text-danger';
      case 'Medium': return 'bg-warning-subtle text-warning-emphasis';
      default: return 'bg-info-subtle text-info';
    }
  }

  openCreateModal() {
    this.ticketForm.reset({ priority: 'Medium' });
    this.isCreateModalOpen.set(true);
  }

  submitTicket() {
    if (this.ticketForm.valid) {
      this.maintenanceService.createRequest(this.ticketForm.getRawValue()).subscribe({
        next: (res) => {
          if (res.succeeded) {
            this.notificationService.success('Maintenance ticket submitted.');
            this.isCreateModalOpen.set(false);
            this.loadRequests();
          }
        }
      });
    }
  }

  openStatusModal(req: MaintenanceRequest) {
    this.selectedRequest.set(req);
    this.statusForm.patchValue({
      status: req.status,
      assignedTechnician: req.assignedTechnician || '',
      resolutionNotes: req.resolutionNotes || ''
    });
    this.isStatusModalOpen.set(true);
  }

  submitStatusUpdate() {
    const req = this.selectedRequest();
    if (req && this.statusForm.valid) {
      this.maintenanceService.updateRequestStatus(req.id, this.statusForm.getRawValue()).subscribe({
        next: (res) => {
          if (res.succeeded) {
            this.notificationService.success('Ticket updated.');
            this.isStatusModalOpen.set(false);
            this.loadRequests();
          }
        }
      });
    }
  }

  deleteTicket(req: MaintenanceRequest) {
    if (confirm(`Are you sure you want to delete maintenance ticket "${req.title}" for Unit ${req.unitNumber}?`)) {
      this.maintenanceService.deleteRequest(req.id).subscribe({
        next: (res) => {
          if (res.succeeded) {
            this.notificationService.success('Maintenance ticket deleted successfully.');
            this.loadRequests();
          } else {
            this.notificationService.error(res.message || 'Failed to delete maintenance ticket.');
          }
        },
        error: (err) => {
          this.notificationService.error(err.error?.message || 'Failed to delete maintenance ticket.');
        }
      });
    }
  }
}
