import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuthStore } from '../../store/auth.store';
import { UserRole } from '../../core/models/auth.models';
import { ToastContainerComponent } from '../../shared/components/toast-container.component';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, ToastContainerComponent],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent {
  public authStore = inject(AuthStore);
  private fb = inject(FormBuilder);

  registerForm = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    phoneNumber: ['', [Validators.pattern(/^$|^(\+251|0)[79]\d{8}$/)]],
    role: ['Admin' as UserRole, [Validators.required]]
  });

  isFieldInvalid(field: string): boolean {
    const control = this.registerForm.get(field);
    return !!(control && control.invalid && (control.dirty || control.touched));
  }

  onSubmit() {
    if (this.registerForm.valid) {
      const raw = this.registerForm.getRawValue();
      this.authStore.register({
        ...raw,
        phoneNumber: raw.phoneNumber && raw.phoneNumber.trim().length > 0 ? raw.phoneNumber.trim() : undefined
      });
    } else {
      this.registerForm.markAllAsTouched();
    }
  }
}
