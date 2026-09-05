import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from '../../store/auth.store';

export const roleGuard: CanActivateFn = (route) => {
  const authStore = inject(AuthStore);
  const router = inject(Router);

  const expectedRoles = route.data?.['roles'] as string[] | undefined;

  if (!expectedRoles || expectedRoles.length === 0) {
    return true;
  }

  const userRoles = authStore.roles();
  const hasRequiredRole = userRoles.some(role => expectedRoles.includes(role));

  if (hasRequiredRole) {
    return true;
  }

  return router.createUrlTree(['/unauthorized']);
};
