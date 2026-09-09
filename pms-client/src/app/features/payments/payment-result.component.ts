import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FinancialService } from '../../core/services/financial.service';

@Component({
  selector: 'app-payment-result',
  standalone: true,
  imports: [CommonModule],
  template: `
    <main class="container py-5 text-center" aria-live="polite">
      <div class="card shadow-sm border-0 mx-auto p-4" style="max-width: 520px;">
        <i class="bi" [class.bi-arrow-repeat]="isVerifying()" [class.bi-check-circle-fill]="succeeded()" [class.bi-exclamation-circle-fill]="!isVerifying() && !succeeded()" [class.text-primary]="isVerifying()" [class.text-success]="succeeded()" [class.text-warning]="!isVerifying() && !succeeded()" style="font-size: 3rem;"></i>
        <h1 class="h3 mt-3">{{ isVerifying() ? 'Verifying your payment' : succeeded() ? 'Payment confirmed' : 'Payment not confirmed yet' }}</h1>
        <p class="text-muted mb-4">{{ message() }}</p>
        <button class="btn btn-primary" (click)="returnToLedger()">Return to financial ledger</button>
      </div>
    </main>
  `
})
export class PaymentResultComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private financialService = inject(FinancialService);

  isVerifying = signal(true);
  succeeded = signal(false);
  message = signal('Please wait while we confirm your payment with Chapa.');

  ngOnInit() {
    const transactionId = this.route.snapshot.queryParamMap.get('transactionId');
    if (!transactionId) {
      this.isVerifying.set(false);
      this.message.set('No payment reference was supplied. Please check the financial ledger.');
      return;
    }

    this.financialService.verifyChapaPayment(transactionId).subscribe({
      next: response => {
        this.isVerifying.set(false);
        this.succeeded.set(response.succeeded);
        this.message.set(response.message);
      },
      error: () => {
        this.isVerifying.set(false);
        this.message.set('We could not verify the payment yet. Please return to the ledger and try again shortly.');
      }
    });
  }

  returnToLedger() { this.router.navigate(['/financial']); }
}
