import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators, FormsModule } from '@angular/forms';
import { PropertyService } from '../../core/services/property.service';
import { Property, PropertyDetail, Unit, UnitStatus, CreateUnitRequest } from '../../core/models/property.models';
import { UserService } from '../../core/services/user.service';
import { UserItem } from '../../core/models/user.models';
import { ModalComponent } from '../../shared/components/modal.component';
import { NotificationService } from '../../core/services/notification.service';
import { AuthStore } from '../../store/auth.store';

@Component({
  selector: 'app-properties',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, ModalComponent],
  templateUrl: './properties.component.html',
  styleUrl: './properties.component.scss'
})
export class PropertiesComponent implements OnInit {
  private propertyService = inject(PropertyService);
  private userService = inject(UserService);
  private notificationService = inject(NotificationService);
  public authStore = inject(AuthStore);
  private fb = inject(FormBuilder);

  // Sub-cities of Addis Ababa
  readonly subCityList: string[] = [
    'Bole',
    'Kirkos',
    'Yeka',
    'Lideta',
    'Nifas Silk-Lafto',
    'Arada',
    'Gullele',
    'Addis Ketema',
    'Akaky Kaliti',
    'Kolfe Keranio',
    'Lemi Kura'
  ];

  // View state: 'buildings' or 'all-units'
  activeView = signal<'buildings' | 'all-units'>('buildings');
  searchFilter: string = '';

  properties = signal<Property[]>([]);
  allUnits = signal<Unit[]>([]);
  propertyManagers = signal<UserItem[]>([]);
  isLoading = signal<boolean>(true);
  isSubmitting = signal<boolean>(false);

  // Property Modals
  isModalOpen = signal<boolean>(false);
  isEditPropertyModalOpen = signal<boolean>(false);
  isDeletePropertyModalOpen = signal<boolean>(false);
  selectedProperty = signal<Property | null>(null);

  // Unit Modals & State
  isUnitsModalOpen = signal<boolean>(false);
  isLoadingUnits = signal<boolean>(false);
  currentUnits = signal<Unit[]>([]);
  isAddUnitModalOpen = signal<boolean>(false);
  isEditUnitModalOpen = signal<boolean>(false);
  selectedUnit = signal<Unit | null>(null);

  // Property Form: Building details with sub-city, without building area
  propertyForm = this.fb.group({
    name: ['', [Validators.required]],
    address: ['', [Validators.required]],
    subCity: ['Bole', [Validators.required]],
    city: ['Addis Ababa', [Validators.required]],
    assignedManagerId: [null as string | null],
    totalFloors: [1, [Validators.min(1)]],
    constructionStatus: ['Completed'],
    finishingNotes: ['']
  });

  editPropertyForm = this.fb.group({
    name: ['', [Validators.required]],
    address: ['', [Validators.required]],
    subCity: ['Bole', [Validators.required]],
    city: ['Addis Ababa', [Validators.required]],
    assignedManagerId: [null as string | null],
    totalFloors: [1, [Validators.min(1)]],
    constructionStatus: ['Completed'],
    finishingNotes: ['']
  });

  // Unit Form: details with price, total area space, which floor
  unitForm = this.fb.nonNullable.group({
    unitNumber: ['', [Validators.required]],
    floorNumber: [1, [Validators.required, Validators.min(0)]],
    squareMeters: [50, [Validators.required, Validators.min(1)]],
    bedrooms: [1, [Validators.required, Validators.min(0)]],
    bathrooms: [1, [Validators.required, Validators.min(1)]],
    rentAmount: [15000, [Validators.required, Validators.min(0)]],
    status: ['Vacant' as UnitStatus, [Validators.required]],
    finishingNotes: ['']
  });

  editUnitForm = this.fb.nonNullable.group({
    floorNumber: [1, [Validators.required, Validators.min(0)]],
    squareMeters: [50, [Validators.required, Validators.min(0)]],
    bedrooms: [1, [Validators.required, Validators.min(0)]],
    bathrooms: [1, [Validators.required, Validators.min(1)]],
    rentAmount: [15000, [Validators.required, Validators.min(0)]],
    status: ['Vacant' as UnitStatus, [Validators.required]],
    finishingNotes: ['']
  });

  ngOnInit() {
    this.refreshData();
    if (this.authStore.isAdmin()) {
      this.loadPropertyManagers();
    }
  }

  loadPropertyManagers() {
    this.userService.getAllUsers().subscribe({
      next: (res) => {
        if (res.succeeded && res.data) {
          const managers = res.data.filter(u => u.roles && u.roles.includes('PropertyManager'));
          this.propertyManagers.set(managers);
        }
      }
    });
  }

  refreshData() {
    this.loadProperties();
    this.loadAllUnits();
  }

  switchToAllUnits() {
    this.activeView.set('all-units');
    this.loadAllUnits();
  }

  loadProperties() {
    this.isLoading.set(true);
    this.propertyService.getProperties().subscribe({
      next: (res) => {
        if (res.succeeded && res.data) {
          this.properties.set(res.data);
        }
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  loadAllUnits() {
    this.propertyService.getUnits().subscribe({
      next: (res) => {
        if (res.succeeded && res.data) {
          this.allUnits.set(res.data);
        }
      }
    });
  }

  filteredProperties(): Property[] {
    const list = this.properties();
    const q = this.searchFilter.trim().toLowerCase();
    if (!q) return list;
    return list.filter(p => 
      p.name.toLowerCase().includes(q) ||
      p.subCity.toLowerCase().includes(q) ||
      p.address.toLowerCase().includes(q) ||
      p.city.toLowerCase().includes(q)
    );
  }

  filteredUnits(): Unit[] {
    const list = this.allUnits();
    const q = this.searchFilter.trim().toLowerCase();
    if (!q) return list;
    return list.filter(u => 
      u.unitNumber.toLowerCase().includes(q) ||
      (u.propertyName && u.propertyName.toLowerCase().includes(q)) ||
      (u.subCity && u.subCity.toLowerCase().includes(q)) ||
      u.status.toLowerCase().includes(q) ||
      u.rentAmount.toString().includes(q)
    );
  }

  getOccupancyRate(property: Property): number {
    if (property.totalUnits === 0) return 0;
    return Math.round((property.occupiedUnits / property.totalUnits) * 100);
  }

  getUnitStatusBadgeClass(status: UnitStatus): string {
    switch (status) {
      case 'Occupied': return 'bg-primary-subtle text-primary border border-primary-subtle';
      case 'Vacant': return 'bg-success-subtle text-success border border-success-subtle';
      case 'Maintenance': return 'bg-warning-subtle text-warning-emphasis border border-warning-subtle';
      case 'UnderFinishing': return 'badge-under-finishing';
      default: return 'bg-secondary-subtle text-secondary border';
    }
  }

  formatUnitStatus(status: UnitStatus): string {
    if (status === 'UnderFinishing') return 'Under Finishing';
    return status;
  }

  getConstructionBadgeClass(status?: string): string {
    switch (status) {
      case 'Completed': return 'bg-success-subtle text-success border border-success-subtle';
      case 'UnderFinishing': return 'bg-warning-subtle text-warning-emphasis border border-warning-subtle';
      case 'Renovation': return 'bg-info-subtle text-info border border-info-subtle';
      default: return 'bg-light text-dark border';
    }
  }

  getConstructionLabel(status?: string): string {
    switch (status) {
      case 'Completed': return 'Finished / Ready';
      case 'UnderFinishing': return 'Under Finishing';
      case 'Renovation': return 'Renovation';
      default: return status || 'Finished';
    }
  }

  openCreateModal() {
    this.propertyForm.reset({
      name: '',
      address: '',
      subCity: 'Bole',
      city: 'Addis Ababa',
      assignedManagerId: null,
      totalFloors: 1,
      constructionStatus: 'Completed',
      finishingNotes: ''
    });
    this.isModalOpen.set(true);
  }

  submitProperty() {
    if (this.propertyForm.valid) {
      this.isSubmitting.set(true);
      const formVal = this.propertyForm.getRawValue();
      this.propertyService.createProperty({
        name: formVal.name || '',
        address: formVal.address || '',
        subCity: formVal.subCity || 'Bole',
        city: formVal.city || 'Addis Ababa',
        assignedManagerId: formVal.assignedManagerId || undefined,
        totalFloors: formVal.totalFloors || 1,
        constructionStatus: formVal.constructionStatus || 'Completed',
        totalSquareMeters: 0,
        finishingNotes: formVal.finishingNotes ? formVal.finishingNotes.trim() : undefined
      }).subscribe({
        next: (res) => {
          this.isSubmitting.set(false);
          if (res.succeeded) {
            this.notificationService.success('Building created successfully.');
            this.isModalOpen.set(false);
            this.refreshData();
          } else {
            this.notificationService.error(res.message || 'Failed to create building.');
          }
        },
        error: () => this.isSubmitting.set(false)
      });
    }
  }

  openEditPropertyModal(prop: Property) {
    this.selectedProperty.set(prop);
    this.editPropertyForm.patchValue({
      name: prop.name,
      address: prop.address,
      subCity: prop.subCity || 'Bole',
      city: prop.city,
      assignedManagerId: prop.assignedManagerId || null,
      totalFloors: prop.totalFloors || 1,
      constructionStatus: prop.constructionStatus || 'Completed',
      finishingNotes: prop.finishingNotes || ''
    });
    this.isEditPropertyModalOpen.set(true);
  }

  submitEditProperty() {
    const prop = this.selectedProperty();
    if (prop && this.editPropertyForm.valid) {
      this.isSubmitting.set(true);
      const formVal = this.editPropertyForm.getRawValue();
      this.propertyService.updateProperty(prop.id, {
        name: formVal.name || '',
        address: formVal.address || '',
        subCity: formVal.subCity || 'Bole',
        city: formVal.city || 'Addis Ababa',
        assignedManagerId: formVal.assignedManagerId || undefined,
        totalFloors: formVal.totalFloors || 1,
        constructionStatus: formVal.constructionStatus || 'Completed',
        totalSquareMeters: prop.totalSquareMeters || 0,
        finishingNotes: formVal.finishingNotes ? formVal.finishingNotes.trim() : undefined
      }).subscribe({
        next: (res) => {
          this.isSubmitting.set(false);
          if (res.succeeded) {
            this.notificationService.success('Building updated successfully.');
            this.isEditPropertyModalOpen.set(false);
            this.refreshData();
          } else {
            this.notificationService.error(res.message || 'Failed to update building.');
          }
        },
        error: () => this.isSubmitting.set(false)
      });
    }
  }

  openDeletePropertyModal(prop: Property) {
    this.selectedProperty.set(prop);
    this.isDeletePropertyModalOpen.set(true);
  }

  confirmDeleteProperty() {
    const prop = this.selectedProperty();
    if (!prop) return;

    this.isSubmitting.set(true);
    this.propertyService.deleteProperty(prop.id).subscribe({
      next: (res) => {
        this.isSubmitting.set(false);
        if (res.succeeded) {
          this.notificationService.success(`Building "${prop.name}" deleted.`);
          this.isDeletePropertyModalOpen.set(false);
          this.refreshData();
        } else {
          this.notificationService.error(res.message || 'Failed to delete building.');
        }
      },
      error: () => this.isSubmitting.set(false)
    });
  }

  // Units Management for Specific Building
  openUnitsModal(prop: Property) {
    this.selectedProperty.set(prop);
    this.isUnitsModalOpen.set(true);
    this.loadUnitsForProperty(prop.id);
  }

  closeUnitsModal() {
    this.isUnitsModalOpen.set(false);
    this.currentUnits.set([]);
  }

  loadUnitsForProperty(propertyId: string) {
    this.isLoadingUnits.set(true);
    this.propertyService.getUnits(propertyId).subscribe({
      next: (res) => {
        if (res.succeeded && res.data) {
          this.currentUnits.set(res.data);
        }
        this.isLoadingUnits.set(false);
      },
      error: () => this.isLoadingUnits.set(false)
    });
  }

  openAddUnitModal() {
    this.unitForm.reset({
      unitNumber: '',
      floorNumber: 1,
      squareMeters: 65,
      bedrooms: 1,
      bathrooms: 1,
      rentAmount: 18000,
      status: 'Vacant',
      finishingNotes: ''
    });
    this.isAddUnitModalOpen.set(true);
  }

  submitAddUnit() {
    const prop = this.selectedProperty();
    if (!prop || this.unitForm.invalid) return;

    this.isSubmitting.set(true);
    const formVal = this.unitForm.getRawValue();
    const req: CreateUnitRequest = {
      propertyId: prop.id,
      unitNumber: formVal.unitNumber.trim(),
      floorNumber: formVal.floorNumber,
      squareMeters: formVal.squareMeters,
      bedrooms: formVal.bedrooms,
      bathrooms: formVal.bathrooms,
      rentAmount: formVal.rentAmount,
      status: formVal.status,
      finishingNotes: formVal.finishingNotes ? formVal.finishingNotes.trim() : undefined
    };

    this.propertyService.createUnit(req).subscribe({
      next: (res) => {
        this.isSubmitting.set(false);
        if (res.succeeded) {
          this.notificationService.success(`Unit ${req.unitNumber} created in ${prop.name}.`);
          this.isAddUnitModalOpen.set(false);
          this.loadUnitsForProperty(prop.id);
          this.refreshData();
        } else {
          this.notificationService.error(res.message || 'Failed to create unit.');
        }
      },
      error: () => this.isSubmitting.set(false)
    });
  }

  openEditUnitModal(unit: Unit) {
    this.selectedUnit.set(unit);
    this.editUnitForm.patchValue({
      floorNumber: unit.floorNumber || 1,
      squareMeters: unit.squareMeters || 50,
      bedrooms: unit.bedrooms,
      bathrooms: unit.bathrooms,
      rentAmount: unit.rentAmount,
      status: unit.status,
      finishingNotes: unit.finishingNotes || ''
    });
    this.isEditUnitModalOpen.set(true);
  }

  submitEditUnit() {
    const unit = this.selectedUnit();
    const prop = this.selectedProperty();
    if (!unit || this.editUnitForm.invalid) return;

    this.isSubmitting.set(true);
    const formVal = this.editUnitForm.getRawValue();

    this.propertyService.updateUnit(unit.id, {
      floorNumber: formVal.floorNumber,
      squareMeters: formVal.squareMeters,
      bedrooms: formVal.bedrooms,
      bathrooms: formVal.bathrooms,
      rentAmount: formVal.rentAmount,
      status: formVal.status,
      finishingNotes: formVal.finishingNotes ? formVal.finishingNotes.trim() : undefined
    }).subscribe({
      next: (res) => {
        this.isSubmitting.set(false);
        if (res.succeeded) {
          this.notificationService.success('Unit updated successfully.');
          this.isEditUnitModalOpen.set(false);
          if (prop) {
            this.loadUnitsForProperty(prop.id);
          }
          this.refreshData();
        } else {
          this.notificationService.error(res.message || 'Failed to update unit.');
        }
      },
      error: () => this.isSubmitting.set(false)
    });
  }

  deleteUnit(unit: Unit) {
    const prop = this.selectedProperty();
    if (confirm(`Are you sure you want to delete Unit ${unit.unitNumber}?`)) {
      this.propertyService.deleteUnit(unit.id).subscribe({
        next: (res) => {
          if (res.succeeded) {
            this.notificationService.success(`Unit ${unit.unitNumber} deleted.`);
            if (prop) {
              this.loadUnitsForProperty(prop.id);
            }
            this.refreshData();
          } else {
            this.notificationService.error(res.message || 'Failed to delete unit.');
          }
        }
      });
    }
  }
}
