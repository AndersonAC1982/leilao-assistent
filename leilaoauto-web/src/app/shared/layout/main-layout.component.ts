import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

interface NavItem {
  label: string;
  path: string;
}

@Component({
  selector: 'app-main-layout',
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss'
})
export class MainLayoutComponent {
  protected readonly navItems: NavItem[] = [
    { label: 'Dashboard', path: '/dashboard' },
    { label: 'Monitoring', path: '/monitoring' },
    { label: 'Lots', path: '/lots' },
    { label: 'Analytics', path: '/analytics' },
    { label: 'Billing', path: '/billing' },
    { label: 'Settings', path: '/settings' }
  ];

  constructor(
    protected readonly authService: AuthService,
    private readonly router: Router
  ) {}

  protected logout(): void {
    this.authService.logout();
    this.router.navigateByUrl('/login');
  }
}
