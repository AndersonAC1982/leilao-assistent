import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { LoginPageComponent } from './features/auth/login-page.component';
import { RegisterPageComponent } from './features/auth/register-page.component';
import { DashboardPageComponent } from './features/dashboard/dashboard-page.component';
import { MonitoringPageComponent } from './features/monitoring/monitoring-page.component';
import { LotsPageComponent } from './features/lots/lots-page.component';
import { AnalyticsPageComponent } from './features/analytics/analytics-page.component';
import { BillingPageComponent } from './features/billing/billing-page.component';
import { SettingsPageComponent } from './features/settings/settings-page.component';
import { MainLayoutComponent } from './shared/layout/main-layout.component';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginPageComponent
  },
  {
    path: 'register',
    component: RegisterPageComponent
  },
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
        component: DashboardPageComponent
      },
      {
        path: 'monitoring',
        component: MonitoringPageComponent
      },
      {
        path: 'lots',
        component: LotsPageComponent
      },
      {
        path: 'analytics',
        component: AnalyticsPageComponent
      },
      {
        path: 'billing',
        component: BillingPageComponent
      },
      {
        path: 'settings',
        component: SettingsPageComponent
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'login'
  }
];
