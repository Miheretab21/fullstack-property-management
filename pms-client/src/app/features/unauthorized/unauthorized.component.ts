import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { AuthStore } from '../../store/auth.store';

@Component({
  selector: 'app-unauthorized',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './unauthorized.component.html',
  styleUrl: './unauthorized.component.scss'
})
export class UnauthorizedComponent {
  public authStore = inject(AuthStore);
  private router = inject(Router);

  goToLogin() {
    this.authStore.logout();
  }

  goToHome() {
    if (this.authStore.isTenant()) {
      this.router.navigate(['/leases']);
    } else {
      this.router.navigate(['/dashboard']);
    }
  }
}
