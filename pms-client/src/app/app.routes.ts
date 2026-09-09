import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { MainLayoutComponent } from './layout/main-layout.component';

export const routes: Routes = [
  // Public Auth Routes
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register.component').then(m => m.RegisterComponent)
  },
  {
    path: 'unauthorized',
    loadComponent: () => import('./features/unauthorized/unauthorized.component').then(m => m.UnauthorizedComponent)
  },
  {
    path: 'payment-result',
    canActivate: [authGuard],
    loadComponent: () => import('./features/payments/payment-result.component').then(m => m.PaymentResultComponent)
  },

  // Authenticated App Shell Routes
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard'
      },
      {
        path: 'dashboard',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'PropertyManager'] },
        loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      {
        path: 'properties',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'PropertyManager', 'Tenant'] },
        loadComponent: () => import('./features/properties/properties.component').then(m => m.PropertiesComponent)
      },
      {
        path: 'leases',
        loadComponent: () => import('./features/leases/leases.component').then(m => m.LeasesComponent)
      },
      {
        path: 'financial',
        loadComponent: () => import('./features/financial/financial.component').then(m => m.FinancialComponent)
      },
      {
        path: 'maintenance',
        loadComponent: () => import('./features/maintenance/maintenance.component').then(m => m.MaintenanceComponent)
      },
      {
        path: 'users',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () => import('./features/users/users.component').then(m => m.UsersComponent)
      }
    ]
  },

  // Fallback
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];
