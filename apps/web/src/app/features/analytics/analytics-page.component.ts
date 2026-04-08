import { CommonModule, CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { ModelAveragePrice, Opportunity, RiskSummary } from '../../core/models/analytics.models';
import { LeilaoApiService } from '../../core/services/leilao-api.service';
import { isValidLotUrl } from '../../shared/utils/lot-url-validator';
import { toDamageLevelLabel, toOpportunityLabel, toRiskDecisionLabel } from '../../shared/utils/scoring-labels';

@Component({
  selector: 'app-analytics-page',
  imports: [CommonModule, ReactiveFormsModule, CurrencyPipe, DecimalPipe],
  templateUrl: './analytics-page.component.html',
  styleUrl: './analytics-page.component.scss'
})
export class AnalyticsPageComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);

  protected readonly loading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly averages = signal<ModelAveragePrice[]>([]);
  protected readonly opportunities = signal<Opportunity[]>([]);
  protected readonly riskSummary = signal<RiskSummary | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    model: ['']
  });

  constructor(private readonly apiService: LeilaoApiService) {}

  ngOnInit(): void {
    this.loadAnalytics();
  }

  protected applyFilter(): void {
    this.loadAnalytics();
  }

  protected clearFilter(): void {
    this.form.setValue({ model: '' });
    this.loadAnalytics();
  }

  protected opportunityLabel(label: string | null | undefined): string {
    return toOpportunityLabel(label);
  }

  protected riskDecisionLabel(decision: string | null | undefined): string {
    return toRiskDecisionLabel(decision);
  }

  protected damageLevelLabel(level: string | null | undefined): string {
    return toDamageLevelLabel(level);
  }

  protected hasValidLotUrl(url: string | null | undefined): boolean {
    return isValidLotUrl(url);
  }

  private loadAnalytics(): void {
    const model = this.form.getRawValue().model.trim();
    const filter = model.length > 0 ? model : undefined;

    this.loading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      averages: this.apiService.getAveragePrice(filter),
      opportunities: this.apiService.getOpportunities(filter),
      riskSummary: this.apiService.getRiskSummary(filter)
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: ({ averages, opportunities, riskSummary }) => {
          this.averages.set(averages);
          this.opportunities.set(opportunities);
          this.riskSummary.set(riskSummary);
        },
        error: () => {
          this.averages.set([]);
          this.opportunities.set([]);
          this.riskSummary.set(null);
          this.errorMessage.set('Não foi possível carregar as análises agora. Tente novamente em alguns segundos.');
        }
      });
  }
}

