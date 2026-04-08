import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { LeilaoApiService } from '@leilaoauto/shared-services';
import type { Lot, ModelPriceRange, OpportunityFeedItem } from '@leilaoauto/shared-types';
import {
  EmptyStateComponent,
  FilterBarComponent,
  LoadingStateComponent,
  OpportunityCardComponent,
  type OpportunityCardViewModel
} from '@leilaoauto/shared-ui';

@Component({
  selector: 'app-lots-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FilterBarComponent,
    OpportunityCardComponent,
    LoadingStateComponent,
    EmptyStateComponent
  ],
  templateUrl: './lots-page.component.html',
  styleUrl: './lots-page.component.scss'
})
export class LotsPageComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);

  protected readonly loading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly activeLots = signal<OpportunityFeedItem[]>([]);
  protected readonly closedLots = signal<Lot[]>([]);
  protected readonly averages = signal<ModelPriceRange[]>([]);

  protected readonly filtersForm = this.formBuilder.group({
    search: [''],
    source: [''],
    minScore: [60],
    vehicleType: [null as number | null],
    region: [''],
    make: [''],
    model: [''],
    year: [null as number | null],
    uf: [''],
    vehicleCondition: [null as number | null]
  });

  protected readonly activeCards = computed<OpportunityCardViewModel[]>(() =>
    this.activeLots().map((item) => ({
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

  protected readonly closedCards = computed<OpportunityCardViewModel[]>(() =>
    this.closedLots().map((item) => ({
      title: item.title,
      source: item.source,
      location: item.uf,
      value: item.finalPrice ?? item.currentBid ?? 0,
      status: item.status,
      score: item.opportunityScore,
      scoreLabel: item.opportunityLabel,
      riskScore: item.riskScore,
      riskDecision: item.riskDecision,
      summary: item.description ?? 'Historico de lote encerrado.',
      lotUrl: item.lotUrl,
      dateUtc: item.updatedAtUtc
    }))
  );

  constructor(private readonly apiService: LeilaoApiService) {}

  ngOnInit(): void {
    this.search();
  }

  protected search(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    const raw = this.filtersForm.getRawValue();

    const opportunitiesQuery = {
      search: raw.search?.trim() || undefined,
      source: raw.source?.trim() || undefined,
      minScore: raw.minScore ?? undefined,
      vehicleType: raw.vehicleType ?? undefined,
      region: (raw.region?.trim() || raw.uf?.trim() || '').toUpperCase() || undefined,
      model: raw.model?.trim() || undefined,
      year: raw.year ?? undefined,
      uf: raw.uf?.trim().toUpperCase() || undefined,
      vehicleCondition: raw.vehicleCondition ?? undefined
    };

    const lotFilter = {
      make: raw.make?.trim() || undefined,
      model: raw.model?.trim() || undefined,
      year: raw.year ?? undefined,
      vehicleType: raw.vehicleType ?? undefined,
      uf: raw.uf?.trim().toUpperCase() || undefined,
      vehicleCondition: raw.vehicleCondition ?? undefined
    };

    forkJoin({
      active: this.apiService.getOpportunitiesFeed(opportunitiesQuery),
      result: this.apiService.searchLots(lotFilter)
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: ({ active, result }) => {
          this.activeLots.set(active);
          this.closedLots.set(result.closedLots);
          this.averages.set(result.averages);
        },
        error: () => {
          this.activeLots.set([]);
          this.closedLots.set([]);
          this.averages.set([]);
          this.errorMessage.set('Nao foi possivel carregar resultados de lotes agora.');
        }
      });
  }

  protected clearFilters(): void {
    this.filtersForm.setValue({
      search: '',
      source: '',
      minScore: 60,
      vehicleType: null,
      region: '',
      make: '',
      model: '',
      year: null,
      uf: '',
      vehicleCondition: null
    });
    this.search();
  }
}
