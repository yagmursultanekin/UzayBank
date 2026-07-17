import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/admin.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full'
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login').then(m => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./features/auth/register/register').then(m => m.RegisterComponent)
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./features/dashboard/dashboard').then(m => m.DashboardComponent),
    canActivate: [authGuard]
  },
  {
    path: 'accounts/:id',
    loadComponent: () =>
      import('./features/accounts/account-detail/account-detail').then(m => m.AccountDetailComponent),
    canActivate: [authGuard]
  },
  {
    path: 'analytics',
    loadComponent: () =>
      import('./features/analytics/analytics').then(m => m.AnalyticsComponent),
    canActivate: [authGuard]
  },
  {
    path: 'nearest-atm',
    loadComponent: () =>
      import('./features/nearest-atm/nearest-atm').then(m => m.NearestAtmComponent),
    canActivate: [authGuard]
  },

  {
  path: 'admin',
  loadComponent: () => import('./features/admin/admin').then(m => m.AdminComponent),
  canActivate: [authGuard,adminGuard]
},
];