import { Routes } from '@angular/router';

// Each page is lazy-loaded, so the first load only ships the shell and the checks list.
export const routes: Routes = [
  {
    path: '',
    title: 'Checks · PulseBoard',
    loadComponent: () => import('./features/checks/checks-page').then((m) => m.ChecksPage),
  },
  {
    path: 'checks/new',
    title: 'Add check · PulseBoard',
    loadComponent: () => import('./features/checks/check-form-page').then((m) => m.CheckFormPage),
  },
  {
    path: 'checks/:id',
    title: 'Check · PulseBoard',
    loadComponent: () => import('./features/checks/check-detail-page').then((m) => m.CheckDetailPage),
  },
  {
    path: 'checks/:id/edit',
    title: 'Edit check · PulseBoard',
    loadComponent: () => import('./features/checks/check-form-page').then((m) => m.CheckFormPage),
  },
  {
    path: 'incidents',
    title: 'Incidents · PulseBoard',
    loadComponent: () => import('./features/incidents/incidents-page').then((m) => m.IncidentsPage),
  },
  { path: '**', redirectTo: '' },
];
