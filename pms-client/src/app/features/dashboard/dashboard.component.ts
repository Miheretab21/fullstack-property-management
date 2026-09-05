import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DashboardService } from '../../core/services/dashboard.service';
import { DashboardMetrics } from '../../core/models/dashboard.models';
import { StatCardComponent } from '../../shared/components/stat-card.component';
import { ModalComponent } from '../../shared/components/modal.component';
import { NotificationService } from '../../core/services/notification.service';
import { AuthStore } from '../../store/auth.store';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, StatCardComponent, ModalComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private dashboardService = inject(DashboardService);
  private notificationService = inject(NotificationService);
  public authStore = inject(AuthStore);

  metrics = signal<DashboardMetrics | null>(null);
  isLoading = signal<boolean>(true);

  // Revenue Target State
  customRevenueTarget = signal<number | null>(null);
  isTargetModalOpen = signal<boolean>(false);
  targetInputIsCustom: boolean = false;
  customTargetInput: number | null = null;

  ngOnInit() {
    const savedTarget = localStorage.getItem('pms_monthly_revenue_target');
    if (savedTarget) {
      const parsed = parseFloat(savedTarget);
      if (!isNaN(parsed) && parsed > 0) {
        this.customRevenueTarget.set(parsed);
      }
    }
    this.loadMetrics();
  }

  loadMetrics() {
    this.isLoading.set(true);
    this.dashboardService.getMetrics().subscribe({
      next: (res) => {
        if (res.succeeded && res.data) {
          this.metrics.set(res.data);
        }
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  isCustomTarget(): boolean {
    const t = this.customRevenueTarget();
    return t !== null && t > 0;
  }

  getEffectiveTarget(): number {
    const custom = this.customRevenueTarget();
    if (custom !== null && custom > 0) {
      return custom;
    }
    return this.metrics()?.totalMonthlyProjectedRevenue || 0;
  }

  openTargetModal() {
    if (this.isCustomTarget()) {
      this.targetInputIsCustom = true;
      this.customTargetInput = this.customRevenueTarget();
    } else {
      this.targetInputIsCustom = false;
      this.customTargetInput = this.metrics()?.totalMonthlyProjectedRevenue || null;
    }
    this.isTargetModalOpen.set(true);
  }

  closeTargetModal() {
    this.isTargetModalOpen.set(false);
  }

  saveTarget() {
    if (this.targetInputIsCustom) {
      if (!this.customTargetInput || this.customTargetInput <= 0) {
        this.notificationService.error('Please enter a valid target amount greater than 0.');
        return;
      }
      this.customRevenueTarget.set(this.customTargetInput);
      localStorage.setItem('pms_monthly_revenue_target', this.customTargetInput.toString());
      this.notificationService.success(`Monthly revenue target set to ETB ${this.customTargetInput.toLocaleString()}.`);
    } else {
      this.customRevenueTarget.set(null);
      localStorage.removeItem('pms_monthly_revenue_target');
      this.notificationService.info('Monthly revenue target reset to automatic lease projection.');
    }
    this.isTargetModalOpen.set(false);
  }

  getOccupiedPct(): number {
    const m = this.metrics();
    if (!m || m.totalUnits === 0) return 0;
    return Math.round((m.occupiedUnits / m.totalUnits) * 100);
  }

  getVacantPct(): number {
    const m = this.metrics();
    if (!m || m.totalUnits === 0) return 0;
    return Math.round((m.vacantUnits / m.totalUnits) * 100);
  }

  getMaintenancePct(): number {
    const m = this.metrics();
    if (!m || m.totalUnits === 0) return 0;
    return Math.round((m.maintenanceUnits / m.totalUnits) * 100);
  }

  getFinishingPct(): number {
    const m = this.metrics();
    if (!m || m.totalUnits === 0) return 0;
    return Math.round(((m.finishingUnits || 0) / m.totalUnits) * 100);
  }

  getCollectionPct(): number {
    const m = this.metrics();
    const target = this.getEffectiveTarget();
    if (!m || target <= 0) return 0;
    return Math.min(100, Math.round((m.currentMonthCollectedRevenue / target) * 100));
  }

  getPendingPct(): number {
    const m = this.metrics();
    const target = this.getEffectiveTarget();
    if (!m || target <= 0) return 0;
    return Math.min(100 - this.getCollectionPct(), Math.round((m.currentMonthPendingRevenue / target) * 100));
  }
}
