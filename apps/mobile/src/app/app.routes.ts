import { Routes } from '@angular/router';
import { authGuard } from '@leilaoauto/shared-services';
import { LoginPageComponent } from './features/auth/login-page.component';
import { OpportunitiesPageComponent } from './features/opportunities/opportunities-page.component';
import { HistoryPageComponent } from './features/history/history-page.component';
import { SettingsPageComponent } from './features/settings/settings-page.component';
import { MainLayoutComponent } from './shared/layout/main-layout.component';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginPageComponent
  },
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'opportunities'
      },
      {
        path: 'opportunities',
        component: OpportunitiesPageComponent
      },
      {
        path: 'history',
        component: HistoryPageComponent
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
