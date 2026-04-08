import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { LeilaoApiService } from '@leilaoauto/shared-services';
import { finalize } from 'rxjs/operators';
import {
  EmptyStateComponent,
  LoadingStateComponent,
  OpportunityCardComponent,
  type OpportunityCardViewModel
} from '@leilaoauto/shared-ui';
import type { OpportunityFeedItem } from '@leilaoauto/shared-types';

@Component({
  selector: 'app-opportunities-page',
  standalone: true,
  imports: [CommonModule, OpportunityCardComponent, EmptyStateComponent, LoadingStateComponent],
  templateUrl: './opportunities-page.component.html',
  styleUrl: './opportunities-page.component.scss'
})
export class OpportunitiesPageComponent implements OnInit {
  protected readonly loading = signal(false);
  protected readonly items = signal<OpportunityFeedItem[]>([]);

  protected readonly cards = computed<OpportunityCardViewModel[]>(() =>
    this.items().map((item) => ({
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

  constructor(private readonly apiService: LeilaoApiService) {}

  ngOnInit(): void {
    this.load();
  }

  protected reload(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.apiService
      .getOpportunitiesFeed({ minScore: 60 })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response: OpportunityFeedItem[]) => this.items.set(response),
        error: () => this.items.set([])
      });
  }
}
