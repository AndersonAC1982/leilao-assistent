import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { LeilaoApiService } from '@leilaoauto/shared-services';

@Component({
  selector: 'app-settings-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './settings-page.component.html',
  styleUrl: './settings-page.component.scss'
})
export class SettingsPageComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);

  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly message = signal<string | null>(null);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    search: [''],
    source: [''],
    minScore: [60, [Validators.min(0), Validators.max(100)]],
    vehicleType: [null as number | null],
    region: [''],
    advancedFiltersEnabled: [false]
  });

  constructor(private readonly apiService: LeilaoApiService) {}

  ngOnInit(): void {
    this.load();
  }

  protected save(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.errorMessage.set(null);
    this.message.set(null);

    const raw = this.form.getRawValue();
    this.apiService
      .updateSettings({
        search: raw.search.trim(),
        source: raw.source.trim(),
        minScore: raw.minScore,
        vehicleType: raw.vehicleType,
        region: raw.region.trim() || null,
        advancedFiltersEnabled: raw.advancedFiltersEnabled
      })
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => this.message.set('Configuracoes salvas com sucesso.'),
        error: (error) => {
          this.errorMessage.set(error?.error?.detail ?? 'Nao foi possivel salvar as configuracoes.');
        }
      });
  }

  private load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.apiService
      .getSettings()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          this.form.setValue({
            search: response.search || '',
            source: response.source || '',
            minScore: response.minScore,
            vehicleType: response.vehicleType ?? null,
            region: response.region ?? '',
            advancedFiltersEnabled: response.advancedFiltersEnabled
          });
        },
        error: (error) => {
          this.errorMessage.set(error?.error?.detail ?? 'Nao foi possivel carregar configuracoes.');
        }
      });
  }
}
