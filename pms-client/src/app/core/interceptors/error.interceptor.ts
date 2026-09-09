import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../services/notification.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const notificationService = inject(NotificationService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        localStorage.removeItem('pms_token');
        localStorage.removeItem('pms_user');
        router.navigate(['/login']);
        notificationService.warning('Session expired. Please log in again.');
      } else if (error.status === 403) {
        router.navigate(['/unauthorized']);
        notificationService.error('You do not have permission to perform this action.');
      } else if (error.status === 400) {
        let errorMessage = 'Validation error.';
        if (error.error?.errors) {
          const validationErrors = error.error.errors;
          const firstKey = Object.keys(validationErrors)[0];
          if (firstKey && Array.isArray(validationErrors[firstKey])) {
            errorMessage = validationErrors[firstKey][0];
          }
        } else if (error.error?.message) {
          errorMessage = error.error.message;
        }
        notificationService.error(errorMessage, 'Request Failed');
      } else if (error.status >= 500) {
        const errorMessage = typeof error.error === 'string'
          ? error.error
          : error.error?.detail || error.error?.message || 'An unexpected server error occurred. Please try again later.';
        notificationService.error(errorMessage);
      }

      return throwError(() => error);
    })
  );
};
