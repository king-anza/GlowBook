import type { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'auth',
    loadComponent: () => import('./features/auth/auth.page').then((m) => m.AuthPage),
  },
  {
    path: 'auth/recovery',
    loadComponent: () => import('./features/auth/recovery.page').then((m) => m.RecoveryPage),
  },
  {
    path: 'account/profile',
    loadComponent: () => import('./features/account/account.page').then((m) => m.AccountPage),
  },
  {
    path: 'home',
    loadComponent: () => import('./features/discovery/discovery.page').then((m) => m.DiscoveryPage),
  },
  {
    path: 'businesses/:id',
    loadComponent: () => import('./features/discovery/business-detail.page').then((m) => m.BusinessDetailPage),
  },
  {
    path: 'book/:staffId/:offeringId',
    loadComponent: () => import('./features/bookings/book.page').then((m) => m.BookPage),
  },
  {
    path: 'bookings',
    loadComponent: () => import('./features/bookings/bookings.page').then((m) => m.BookingsPage),
  },
  {
    path: 'pricing',
    loadComponent: () => import('./features/pricing/pricing.page').then((m) => m.PricingPage),
  },
  {
    path: 'professional/profile',
    loadComponent: () => import('./features/professional/profile.page').then((m) => m.ProfilePage),
  },
  {
    path: 'professional/services',
    loadComponent: () => import('./features/professional/services.page').then((m) => m.ServicesPage),
  },
  {
    path: 'professional/availability',
    loadComponent: () => import('./features/professional/availability.page').then((m) => m.AvailabilityPage),
  },
  {
    path: 'professional/bookings',
    loadComponent: () =>
      import('./features/professional/professional-bookings.page').then((m) => m.ProfessionalBookingsPage),
  },
  {
    path: 'admin',
    loadComponent: () => import('./features/admin/admin.page').then((m) => m.AdminPage),
  },
  {
    path: '',
    redirectTo: 'home',
    pathMatch: 'full',
  },
];
