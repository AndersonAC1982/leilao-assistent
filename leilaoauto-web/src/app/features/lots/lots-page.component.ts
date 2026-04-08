import { CommonModule, CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { Lot, LotSearchFilterRequest, ModelPriceRange } from '../../core/models/lot.models';
import { LeilaoApiService } from '../../core/services/leilao-api.service';
import { isValidLotUrl } from '../../shared/utils/lot-url-validator';
import { toOpportunityLabel, toRiskDecisionLabel } from '../../shared/utils/scoring-labels';

@Component({
  selector: 'app-lots-page',
  imports: [CommonModule, ReactiveFormsModule, CurrencyPipe, DecimalPipe],
  templateUrl: './lots-page.component.html',
  styleUrl: './lots-page.component.scss'
})
export class LotsPageComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);

  protected readonly loading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly activeLots = signal<Lot[]>([]);
  protected readonly closedLots = signal<Lot[]>([]);
  protected readonly averages = signal<ModelPriceRange[]>([]);

  protected readonly filtersForm = this.formBuilder.group({
    make: [''],
    model: [''],
    year: [null as number | null],
    vehicleType: [null as number | null],
    uf: [''],
    vehicleCondition: [null as number | null]
  });

  protected readonly statusLabels: Record<number, string> = {
    0: 'RASCUNHO',
    1: 'ATIVO',
    2: 'ENCERRADO',
    3: 'CONFIRMADO'
  };

  constructor(private readonly apiService: LeilaoApiService) {}

  ngOnInit(): void {
    this.search();
  }

  protected search(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    const filter = this.buildFilter();
    this.apiService
      .searchLots(filter)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (result) => {
          this.activeLots.set(result.activeLots);
          this.closedLots.set(result.closedLots);
          this.averages.set(result.averages);
        },
        error: () => {
          this.activeLots.set([]);
          this.closedLots.set([]);
          this.averages.set([]);
          this.errorMessage.set('Não foi possível carregar os resultados agora.');
        }
      });
  }

  protected clearFilters(): void {
    this.filtersForm.setValue({
      make: '',
      model: '',
      year: null,
      vehicleType: null,
      uf: '',
      vehicleCondition: null
    });

    this.search();
  }

  protected hasValidLotUrl(url: string | null | undefined): boolean {
    return isValidLotUrl(url);
  }

  protected trackByLot(index: number, lot: Lot): string {
    return lot.id;
  }

  protected trackByAverage(index: number, item: ModelPriceRange): string {
    return item.comparableModel;
  }

  protected opportunityLabel(label: string | null | undefined): string {
    return toOpportunityLabel(label);
  }

  protected riskDecisionLabel(decision: string | null | undefined): string {
    return toRiskDecisionLabel(decision);
  }

  private buildFilter(): LotSearchFilterRequest {
    const raw = this.filtersForm.getRawValue();

    return {
      make: raw.make?.trim() || undefined,
      model: raw.model?.trim() || undefined,
      year: raw.year ?? undefined,
      vehicleType: raw.vehicleType ?? undefined,
      uf: raw.uf?.trim().toUpperCase() || undefined,
      vehicleCondition: raw.vehicleCondition ?? undefined
    };
  }
}

