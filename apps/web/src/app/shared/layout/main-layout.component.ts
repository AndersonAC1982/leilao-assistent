import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '@leilaoauto/shared-services';
import { PlanBadgeComponent } from '@leilaoauto/shared-ui';

interface NavItem {
  label: string;
  path: string;
}

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, PlanBadgeComponent],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss'
})
export class MainLayoutComponent {
  protected readonly navItems: NavItem[] = [
    { label: 'Painel', path: '/dashboard' },
    { label: 'Oportunidades', path: '/opportunities' },
    { label: 'Historico', path: '/history' },
    { label: 'Monitoramento', path: '/monitoring' },
    { label: 'Assinatura', path: '/billing' },
    { label: 'Configuracoes', path: '/settings' }
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
