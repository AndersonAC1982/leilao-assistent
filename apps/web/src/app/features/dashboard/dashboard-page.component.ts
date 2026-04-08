import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { AuthService, LeilaoApiService } from '@leilaoauto/shared-services';
import type { HistoryItem, OpportunityFeedItem } from '@leilaoauto/shared-types';
import {
  AppHeaderComponent,
  EmptyStateComponent,
  HistoryListComponent,
  LoadingStateComponent,
  OpportunityCardComponent,
  type OpportunityCardViewModel,
  StatusChip,
  StatusChipsComponent
} from '@leilaoauto/shared-ui';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [
    CommonModule,
    AppHeaderComponent,
    StatusChipsComponent,
    OpportunityCardComponent,
    HistoryListComponent,
    EmptyStateComponent,
    LoadingStateComponent
  ],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss'
})
export class DashboardPageComponent implements OnInit {
  protected readonly loading = signal(false);
  protected readonly running = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly infoMessage = signal<string | null>(null);

  protected readonly opportunities = signal<OpportunityFeedItem[]>([]);
  protected readonly history = signal<HistoryItem[]>([]);

  protected readonly statusChips = computed<StatusChip[]>(() => {
    const items = this.opportunities();
    const sources = new Set(items.map((item) => item.source));
    const maxScore = items.length > 0 ? Math.max(...items.map((item) => item.score)) : 0;
    const strong = items.filter((item) => item.score >= 75).length;

    return [
      { label: 'Fontes ativas', value: String(sources.size), tone: 'neutral' },
      { label: 'Lotes encontrados', value: String(items.length), tone: 'neutral' },
      { label: 'Maior score', value: maxScore.toFixed(1), tone: maxScore >= 75 ? 'good' : 'warn' },
      { label: 'Oportunidades fortes', value: String(strong), tone: strong > 0 ? 'good' : 'warn' },
      { label: 'Status', value: this.running() ? 'Varrendo' : 'Pronto', tone: this.running() ? 'warn' : 'good' }
    ];
  });

  protected readonly topCards = computed<OpportunityCardViewModel[]>(() =>
    this.opportunities()
      .slice(0, 6)
      .map((item) => ({
        title: item.title,
        source: item.source,
        location: item.location,
        value: item.value,
        status: item.status,
        score: item.score,
        scoreLabel: item.scoreLabel,
        riskScore: item.riskScore,
        riskDecision: item.riskDecision,
        summary: item.summary,
        lotUrl: item.lotUrl,
        dateUtc: item.dateUtc
      }))
  );

  constructor(
    protected readonly authService: AuthService,
    private readonly apiService: LeilaoApiService
  ) {}

  ngOnInit(): void {
    this.reload();
  }

  protected runNow(): void {
    this.running.set(true);
    this.infoMessage.set(null);
    this.errorMessage.set(null);

    this.apiService
      .runScanner()
      .pipe(finalize(() => this.running.set(false)))
      .subscribe({
        next: (response) => {
          this.infoMessage.set(response.message);
          this.reload();
        },
        error: (error) => {
          this.errorMessage.set(error?.error?.detail ?? 'Falha ao rodar varredura agora.');
        }
      });
  }

  private reload(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      opportunities: this.apiService.getOpportunitiesFeed({ minScore: 0 }),
      history: this.apiService.getExecutionHistory(8)
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: ({ opportunities, history }) => {
          this.opportunities.set(opportunities);
          this.history.set(history);
        },
        error: () => {
          this.opportunities.set([]);
          this.history.set([]);
          this.errorMessage.set('Nao foi possivel carregar os dados operacionais do painel.');
        }
      });
  }
}
