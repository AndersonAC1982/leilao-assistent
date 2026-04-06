import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-lots-page',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './lots-page.component.html',
  styleUrl: './lots-page.component.scss'
})
export class LotsPageComponent {
  private readonly formBuilder = inject(FormBuilder);

  protected readonly filtersForm = this.formBuilder.group({
    make: [''],
    model: [''],
    yearFrom: [null as number | null],
    yearTo: [null as number | null],
    vehicleType: [''],
    uf: ['']
  });

  protected readonly filterPreview = signal<string>('No filters applied yet.');

  protected applyFilters(): void {
    this.filterPreview.set(JSON.stringify(this.filtersForm.getRawValue()));
  }
}
