import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { MonitoredVehicle } from '../../core/models/monitoring.models';
import { LeilaoApiService } from '../../core/services/leilao-api.service';

interface SelectOption {
  value: number;
  label: string;
}

@Component({
  selector: 'app-monitoring-page',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './monitoring-page.component.html',
  styleUrl: './monitoring-page.component.scss'
})
export class MonitoringPageComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);

  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly vehicles = signal<MonitoredVehicle[]>([]);
  protected readonly editingId = signal<string | null>(null);

  protected readonly typeOptions: SelectOption[] = [
    { value: 1, label: 'Car' },
    { value: 2, label: 'Motorcycle' },
    { value: 3, label: 'Truck' },
    { value: 4, label: 'Utility' },
    { value: 5, label: 'Other' }
  ];

  protected readonly stateOptions: SelectOption[] = [
    { value: 0, label: 'Unknown' },
    { value: 1, label: 'Running' },
    { value: 2, label: 'Damaged' },
    { value: 3, label: 'Flooded' },
    { value: 4, label: 'Theft Recovery' },
    { value: 5, label: 'Scrap' }
  ];

  protected readonly form = this.formBuilder.nonNullable.group({
    brand: ['', [Validators.required, Validators.maxLength(60)]],
    model: ['', [Validators.required, Validators.maxLength(100)]],
    year: [new Date().getFullYear(), [Validators.required, Validators.min(1960), Validators.max(new Date().getFullYear() + 1)]],
    type: [1, [Validators.required]],
    uf: ['SP', [Validators.required, Validators.minLength(2), Validators.maxLength(2)]],
    vehicleState: [1, [Validators.required]]
  });

  constructor(private readonly apiService: LeilaoApiService) {}

  ngOnInit(): void {
    this.loadVehicles();
  }

  protected trackByVehicle(index: number, vehicle: MonitoredVehicle): string {
    return vehicle.id;
  }

  protected canCreateMore(): boolean {
    return this.editingId() !== null || this.vehicles().length < 4;
  }

  protected submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    if (!this.canCreateMore()) {
      this.errorMessage.set('You can monitor up to 4 vehicles. Remove one before creating another.');
      return;
    }

    this.saving.set(true);
    this.errorMessage.set(null);

    const raw = this.form.getRawValue();
    const payload = {
      brand: raw.brand.trim(),
      model: raw.model.trim(),
      year: Number(raw.year),
      type: Number(raw.type),
      uf: raw.uf.trim().toUpperCase(),
      vehicleState: Number(raw.vehicleState)
    };

    const editingId = this.editingId();
    const action = editingId
      ? this.apiService.updateMonitoredVehicle(editingId, payload)
      : this.apiService.addMonitoredVehicle(payload);

    action.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: () => {
        this.cancelEdit();
        this.loadVehicles();
      },
      error: () => {
        this.errorMessage.set('Could not save vehicle. Verify fields and try again.');
      }
    });
  }

  protected editVehicle(vehicle: MonitoredVehicle): void {
    this.editingId.set(vehicle.id);
    this.errorMessage.set(null);
    this.form.setValue({
      brand: vehicle.brand,
      model: vehicle.model,
      year: vehicle.year,
      type: vehicle.type,
      uf: vehicle.uf,
      vehicleState: vehicle.vehicleState
    });
  }

  protected cancelEdit(): void {
    this.editingId.set(null);
    this.form.setValue({
      brand: '',
      model: '',
      year: new Date().getFullYear(),
      type: 1,
      uf: 'SP',
      vehicleState: 1
    });
  }

  protected removeVehicle(vehicleId: string): void {
    if (this.saving()) {
      return;
    }

    this.saving.set(true);
    this.errorMessage.set(null);

    this.apiService
      .removeMonitoredVehicle(vehicleId)
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          if (this.editingId() === vehicleId) {
            this.cancelEdit();
          }

          this.loadVehicles();
        },
        error: () => {
          this.errorMessage.set('Could not remove vehicle right now.');
        }
      });
  }

  protected typeLabel(type: number): string {
    return this.typeOptions.find((option) => option.value === type)?.label ?? String(type);
  }

  protected stateLabel(state: number): string {
    return this.stateOptions.find((option) => option.value === state)?.label ?? String(state);
  }

  private loadVehicles(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.apiService
      .getMonitoredVehicles()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => this.vehicles.set(response),
        error: () => {
          this.vehicles.set([]);
          this.errorMessage.set('Could not load your monitored vehicles.');
        }
      });
  }
}
