import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { LeilaoApiService } from '@leilaoauto/shared-services';
import type { UserSettings } from '@leilaoauto/shared-types';

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
  protected readonly status = signal<string>('');

  protected readonly form = this.formBuilder.nonNullable.group({
    search: [''],
    source: [''],
    minScore: [60],
    region: ['']
  });

  constructor(private readonly apiService: LeilaoApiService) {}

  ngOnInit(): void {
    this.loading.set(true);
    this.apiService
      .getSettings()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (settings: UserSettings) =>
          this.form.setValue({
            search: settings.search,
            source: settings.source,
            minScore: settings.minScore,
            region: settings.region || ''
          }),
        error: () => {
          this.status.set('Falha ao carregar configuracoes.');
        }
      });
  }

  protected save(): void {
    this.saving.set(true);
    this.status.set('');

    const raw = this.form.getRawValue();

    this.apiService
      .updateSettings({
        search: raw.search,
        source: raw.source,
        minScore: raw.minScore,
        vehicleType: null,
        region: raw.region || null,
        advancedFiltersEnabled: true
      })
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.status.set('Configuracoes salvas.');
        },
        error: () => {
          this.status.set('Falha ao salvar configuracoes.');
        }
      });
  }
}
