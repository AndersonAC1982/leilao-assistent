import { Routes } from '@angular/router';
import { authGuard } from '@leilaoauto/shared-services';
import { LoginPageComponent } from './features/auth/login-page.component';
import { RegisterPageComponent } from './features/auth/register-page.component';
import { DashboardPageComponent } from './features/dashboard/dashboard-page.component';
import { MonitoringPageComponent } from './features/monitoring/monitoring-page.component';
import { LotsPageComponent } from './features/lots/lots-page.component';
import { BillingPageComponent } from './features/billing/billing-page.component';
import { SettingsPageComponent } from './features/settings/settings-page.component';
import { MainLayoutComponent } from './shared/layout/main-layout.component';
import { HistoryPageComponent } from './features/history/history-page.component';

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
        path: 'opportunities',
        component: LotsPageComponent
      },
      {
        path: 'lots',
        redirectTo: 'opportunities',
        pathMatch: 'full'
      },
      {
        path: 'monitoring',
        component: MonitoringPageComponent
      },
      {
        path: 'history',
        component: HistoryPageComponent
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
