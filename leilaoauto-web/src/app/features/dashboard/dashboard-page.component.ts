import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-dashboard-page',
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss'
})
export class DashboardPageComponent {
  protected readonly roleLabels: Record<number, string> = {
    1: 'User',
    2: 'Admin'
  };

  protected readonly planLabels: Record<number, string> = {
    1: 'Free',
    2: 'Pro',
    3: 'Enterprise'
  };

  constructor(protected readonly authService: AuthService) {}
}
