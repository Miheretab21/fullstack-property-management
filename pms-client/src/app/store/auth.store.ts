import { computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { AuthResponse, LoginRequest, RegisterRequest, UserProfile } from '../core/models/auth.models';
import { AuthService } from '../core/services/auth.service';
import { NotificationService } from '../core/services/notification.service';

interface AuthState {
  token: string | null;
  user: UserProfile | null;
  isLoading: boolean;
  error: string | null;
}

const getSavedToken = (): string | null => {
  try {
    return localStorage.getItem('pms_token');
  } catch {
    return null;
  }
};

const getSavedUser = (): UserProfile | null => {
  try {
    const raw = localStorage.getItem('pms_user');
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
};

const initialState: AuthState = {
  token: getSavedToken(),
  user: getSavedUser(),
  isLoading: false,
  error: null
};

export const AuthStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withComputed((store) => ({
    isAuthenticated: computed(() => !!store.token()),
    currentUser: computed(() => store.user()),
    roles: computed(() => store.user()?.roles ?? []),
    isAdmin: computed(() => store.user()?.roles.includes('Admin') ?? false),
    isPropertyManager: computed(() => store.user()?.roles.includes('PropertyManager') ?? false),
    isTenant: computed(() => store.user()?.roles.includes('Tenant') ?? false),
    fullName: computed(() => {
      const u = store.user();
      return u ? `${u.firstName} ${u.lastName}`.trim() : 'User';
    })
  })),
  withMethods((store, authService = inject(AuthService), notificationService = inject(NotificationService), router = inject(Router)) => ({
    async login(credentials: LoginRequest, returnUrl = '/dashboard'): Promise<boolean> {
      patchState(store, { isLoading: true, error: null });

      try {
        const result = await firstValueFrom(authService.login(credentials));

        if (result.succeeded && result.data) {
          const authData = result.data;
          localStorage.setItem('pms_token', authData.token);

          const userProfile: UserProfile = {
            id: authData.userId,
            email: authData.email,
            firstName: authData.firstName,
            lastName: authData.lastName,
            roles: authData.roles,
            isActive: true,
            createdAtUtc: new Date().toISOString()
          };

          localStorage.setItem('pms_user', JSON.stringify(userProfile));

          patchState(store, {
            token: authData.token,
            user: userProfile,
            isLoading: false,
            error: null
          });

          notificationService.success(`Welcome back, ${userProfile.firstName}!`);
          
          // Redirect tenants to leases or maintenance; admins/managers to dashboard
          const targetUrl = userProfile.roles.includes('Tenant') && !userProfile.roles.includes('Admin')
            ? '/leases' 
            : returnUrl;

          router.navigateByUrl(targetUrl);
          return true;
        } else {
          patchState(store, { isLoading: false, error: result.message });
          notificationService.error(result.message || 'Login failed');
          return false;
        }
      } catch (err: any) {
        const errorMsg = err.error?.message || 'Login failed. Please verify your credentials.';
        patchState(store, { isLoading: false, error: errorMsg });
        return false;
      }
    },

    async register(data: RegisterRequest): Promise<boolean> {
      patchState(store, { isLoading: true, error: null });

      try {
        const result = await firstValueFrom(authService.register(data));

        if (result.succeeded && result.data) {
          const authData = result.data;
          localStorage.setItem('pms_token', authData.token);

          const userProfile: UserProfile = {
            id: authData.userId,
            email: authData.email,
            firstName: authData.firstName,
            lastName: authData.lastName,
            roles: authData.roles,
            isActive: true,
            createdAtUtc: new Date().toISOString()
          };

          localStorage.setItem('pms_user', JSON.stringify(userProfile));

          patchState(store, {
            token: authData.token,
            user: userProfile,
            isLoading: false,
            error: null
          });

          notificationService.success('Registration successful!');
          router.navigate(['/dashboard']);
          return true;
        } else {
          patchState(store, { isLoading: false, error: result.message });
          notificationService.error(result.message || 'Registration failed');
          return false;
        }
      } catch (err: any) {
        const errorMsg = err.error?.message || 'Registration failed. Please try again.';
        patchState(store, { isLoading: false, error: errorMsg });
        return false;
      }
    },

    logout() {
      localStorage.removeItem('pms_token');
      localStorage.removeItem('pms_user');

      patchState(store, {
        token: null,
        user: null,
        isLoading: false,
        error: null
      });

      notificationService.info('You have been logged out.');
      router.navigate(['/login']);
    },

    async refreshProfile() {
      if (!store.token()) return;

      try {
        const result = await firstValueFrom(authService.getCurrentUser());
        if (result.succeeded && result.data) {
          localStorage.setItem('pms_user', JSON.stringify(result.data));
          patchState(store, { user: result.data });
        }
      } catch {
        // Token may be invalid; handled by error interceptor
      }
    }
  }))
);
