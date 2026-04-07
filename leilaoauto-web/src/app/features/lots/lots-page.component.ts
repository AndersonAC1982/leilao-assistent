import { CommonModule, CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { Lot, LotSearchFilterRequest } from '../../core/models/lot.models';
import { LeilaoApiService } from '../../core/services/leilao-api.service';

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
  protected readonly lots = signal<Lot[]>([]);

  protected readonly filtersForm = this.formBuilder.group({
    make: [''],
    model: [''],
    yearFrom: [null as number | null],
    yearTo: [null as number | null],
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

  ngOnInit(): void {
    this.searchActive();
  }

  constructor(private readonly apiService: LeilaoApiService) {}

  protected applyFilters(): void {
    this.searchActive();
  }

  protected clearFilters(): void {
    this.filtersForm.setValue({
      make: '',
      model: '',
      yearFrom: null,
      yearTo: null,
      vehicleType: null,
      uf: '',
      vehicleCondition: null
    });

    this.searchActive();
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

  protected trackByLot(index: number, lot: Lot): string {
    return lot.id;
  }

  private searchActive(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    const raw = this.filtersForm.getRawValue();
    const filter: LotSearchFilterRequest = {
      make: raw.make?.trim() || undefined,
      model: raw.model?.trim() || undefined,
      yearFrom: raw.yearFrom ?? undefined,
      yearTo: raw.yearTo ?? undefined,
      vehicleType: raw.vehicleType ?? undefined,
      uf: raw.uf?.trim().toUpperCase() || undefined,
      vehicleCondition: raw.vehicleCondition ?? undefined
    };

    this.apiService
      .searchActiveLots(filter)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => this.lots.set(response),
        error: () => {
          this.lots.set([]);
          this.errorMessage.set('Nao foi possivel carregar os lotes no momento.');
        }
      });
  }
}
