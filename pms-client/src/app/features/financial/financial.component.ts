import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators, FormsModule } from '@angular/forms';
import { FinancialService } from '../../core/services/financial.service';
import { Transaction } from '../../core/models/financial.models';
import { LeaseService } from '../../core/services/lease.service';
import { Lease } from '../../core/models/lease.models';
import { ModalComponent } from '../../shared/components/modal.component';
import { NotificationService } from '../../core/services/notification.service';
import { AuthStore } from '../../store/auth.store';

@Component({
  selector: 'app-financial',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, ModalComponent],
  templateUrl: './financial.component.html',
  styleUrl: './financial.component.scss'
})
export class FinancialComponent implements OnInit {
  private financialService = inject(FinancialService);
  private leaseService = inject(LeaseService);
  private notificationService = inject(NotificationService);
  public authStore = inject(AuthStore);
  private fb = inject(FormBuilder);

  transactions = signal<Transaction[]>([]);
  activeLeases = signal<Lease[]>([]);
  isLoading = signal<boolean>(true);
  isSubmitting = signal<boolean>(false);

  isPayModalOpen = signal<boolean>(false);
  selectedTx = signal<Transaction | null>(null);

  isDirectModalOpen = signal<boolean>(false);

  paymentForm = this.fb.nonNullable.group({
    paymentMethod: ['Check', [Validators.required]],
    checkNumber: [''],
    bankName: ['Commercial Bank of Ethiopia']
  });

  directPaymentForm = this.fb.nonNullable.group({
    leaseId: ['', [Validators.required]],
    amount: [0, [Validators.required, Validators.min(1)]],
    paymentMethod: ['Check', [Validators.required]],
    checkNumber: [''],
    bankName: ['Commercial Bank of Ethiopia'],
    isCleared: [true]
  });

  ngOnInit() {
    this.loadTransactions();
    this.loadActiveLeases();
  }

  loadTransactions() {
    this.isLoading.set(true);
    this.financialService.getTransactions().subscribe({
      next: (res) => {
        if (res.succeeded && res.data) {
          this.transactions.set(res.data);
        }
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  loadActiveLeases() {
    this.leaseService.getLeases().subscribe({
      next: (res) => {
        if (res.succeeded && res.data) {
          this.activeLeases.set(res.data.filter(l => l.isActive));
        }
      }
    });
  }

  isCheckMethod(method?: string): boolean {
    return !!method && method.toLowerCase().includes('check');
  }

  openPaymentModal(tx: Transaction) {
    this.selectedTx.set(tx);
    this.paymentForm.reset({
      paymentMethod: 'Check',
      checkNumber: '',
      bankName: 'Commercial Bank of Ethiopia'
    });
    this.isPayModalOpen.set(true);
  }

  submitPayment() {
    const tx = this.selectedTx();
    if (tx && this.paymentForm.valid) {
      this.isSubmitting.set(true);
      const val = this.paymentForm.getRawValue();

      this.financialService.recordPayment({
        transactionId: tx.id,
        paymentMethod: val.paymentMethod,
        checkNumber: val.checkNumber?.trim() || undefined,
        bankName: val.bankName?.trim() || undefined
      }).subscribe({
        next: (res) => {
          this.isSubmitting.set(false);
          if (res.succeeded) {
            this.notificationService.success('Payment recorded successfully! Marked as Paid.');
            this.isPayModalOpen.set(false);
            this.loadTransactions();
          } else {
            this.notificationService.error(res.message || 'Failed to record payment.');
          }
        },
        error: () => this.isSubmitting.set(false)
      });
    }
  }

  payWithChapa(tx: Transaction) {
    this.isSubmitting.set(true);
    this.financialService.initializeChapaPayment(tx.id).subscribe({
      next: (response) => {
        // A full-page redirect is required for Chapa's hosted checkout.
        window.location.assign(response.checkoutUrl);
      },
      error: () => {
        this.isSubmitting.set(false);
      }
    });
  }

  verifyChapaPayment(tx: Transaction) {
    this.isSubmitting.set(true);
    this.financialService.verifyChapaPayment(tx.id).subscribe({
      next: (response) => {
        this.isSubmitting.set(false);
        if (response.succeeded) {
          this.notificationService.success('Your Chapa payment has been verified.');
          this.loadTransactions();
        } else {
          this.notificationService.info(response.message || 'Chapa is still processing this payment. Please try again shortly.');
        }
      },
      error: () => this.isSubmitting.set(false)
    });
  }

  openDirectPaymentModal() {
    this.directPaymentForm.reset({
      leaseId: '',
      amount: 0,
      paymentMethod: 'Check',
      checkNumber: '',
      bankName: 'Commercial Bank of Ethiopia',
      isCleared: true
    });
    this.isDirectModalOpen.set(true);
  }

  onLeaseSelect(event: Event) {
    const leaseId = (event.target as HTMLSelectElement).value;
    const lease = this.activeLeases().find(l => l.id === leaseId);
    if (lease) {
      this.directPaymentForm.patchValue({
        amount: lease.monthlyRent
      });
    }
  }

  submitDirectPayment() {
    if (this.directPaymentForm.invalid) return;

    this.isSubmitting.set(true);
    const val = this.directPaymentForm.getRawValue();

    this.financialService.createDirectPayment({
      leaseId: val.leaseId,
      amount: Number(val.amount),
      paymentMethod: val.paymentMethod,
      checkNumber: val.checkNumber.trim() || undefined,
      bankName: val.bankName.trim() || undefined,
      isCleared: val.isCleared,
      paymentDate: new Date().toISOString()
    }).subscribe({
      next: (res) => {
        this.isSubmitting.set(false);
        if (res.succeeded) {
          const statusText = val.isCleared ? 'Paid (Money Received)' : 'Pending Clearance';
          this.notificationService.success(`Payment of ETB ${val.amount.toLocaleString()} recorded as ${statusText}!`);
          this.isDirectModalOpen.set(false);
          this.loadTransactions();
        } else {
          this.notificationService.error(res.message || 'Failed to record payment.');
        }
      },
      error: () => this.isSubmitting.set(false)
    });
  }
}
