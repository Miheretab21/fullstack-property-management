import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuthStore } from '../../store/auth.store';
import { ToastContainerComponent } from '../../shared/components/toast-container.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, ToastContainerComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  public authStore = inject(AuthStore);
  private fb = inject(FormBuilder);

  showPassword = false;

  loginForm = this.fb.nonNullable.group({
    email: ['admin@propertymgt.com', [Validators.required, Validators.email]],
    password: ['Admin@123456', [Validators.required, Validators.minLength(6)]]
  });

  isFieldInvalid(field: 'email' | 'password'): boolean {
    const control = this.loginForm.get(field);
    return !!(control && control.invalid && (control.dirty || control.touched));
  }

  fillDemo(email: string, pass: string) {
    this.loginForm.patchValue({ email, password: pass });
  }

  onSubmit() {
    if (this.loginForm.valid) {
      this.authStore.login(this.loginForm.getRawValue());
    } else {
      this.loginForm.markAllAsTouched();
    }
  }
}
