import { CommonModule, CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { Lot } from '../../core/models/lot.models';
import { AuthService } from '../../core/services/auth.service';
import { LeilaoApiService } from '../../core/services/leilao-api.service';
import { isValidLotUrl } from '../../shared/utils/lot-url-validator';
import { toOpportunityLabel, toRiskDecisionLabel } from '../../shared/utils/scoring-labels';

@Component({
  selector: 'app-dashboard-page',
  imports: [CommonModule, RouterLink, CurrencyPipe, DecimalPipe],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss'
})
export class DashboardPageComponent implements OnInit {
  protected readonly roleLabels: Record<number, string> = {
    1: 'Usuário',
    2: 'Administrador'
  };

  protected readonly planLabels: Record<number, string> = {
    1: 'Grátis',
    2: 'Pro',
    3: 'Premium',
    4: 'Elite'
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
    return isValidLotUrl(url);
  }

  protected opportunityLabel(label: string | null | undefined): string {
    return toOpportunityLabel(label);
  }

  protected riskDecisionLabel(decision: string | null | undefined): string {
    return toRiskDecisionLabel(decision);
  }
}

