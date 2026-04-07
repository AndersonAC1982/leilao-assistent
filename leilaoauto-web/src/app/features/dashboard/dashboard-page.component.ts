import { CommonModule, CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { Lot } from '../../core/models/lot.models';
import { AuthService } from '../../core/services/auth.service';
import { LeilaoApiService } from '../../core/services/leilao-api.service';

@Component({
  selector: 'app-dashboard-page',
  imports: [CommonModule, RouterLink, CurrencyPipe, DecimalPipe],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss'
})
export class DashboardPageComponent implements OnInit {
  protected readonly roleLabels: Record<number, string> = {
    1: 'User',
    2: 'Admin'
  };

  protected readonly planLabels: Record<number, string> = {
    1: 'Free',
    2: 'Pro',
    3: 'Enterprise'
  };

  protected readonly statusLabels: Record<number, string> = {
    0: 'RASCUNHO',
    1: 'ATIVO',
    2: 'ENCERRADO',
    3: 'CONFIRMADO'
  };

  protected readonly loadingLots = signal(false);
  protected readonly lotCards = signal<Lot[]>([]);

  constructor(
    protected readonly authService: AuthService,
    private readonly apiService: LeilaoApiService
  ) {}

  ngOnInit(): void {
    this.loadingLots.set(true);

    this.apiService
      .searchActiveLots({})
      .pipe(finalize(() => this.loadingLots.set(false)))
      .subscribe({
        next: (lots) => this.lotCards.set(lots.slice(0, 3)),
        error: () => this.lotCards.set([])
      });
  }

  protected hasValidLotUrl(url: string | null | undefined): boolean {
    if (!url) {
      return false;
    }

    try {
      const parsed = new URL(url);
      const path = parsed.pathname.replace(/\/+$/, '').toLowerCase();
      if (!path || path === '/' || path === '/home' || path === '/inicio' || path === '/index' || path === '/default') {
        return false;
      }

      return /\d/.test(parsed.pathname + parsed.search);
    } catch {
      return false;
    }
  }
}
