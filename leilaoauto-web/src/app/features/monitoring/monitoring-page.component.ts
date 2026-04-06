import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-monitoring-page',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './monitoring-page.component.html',
  styleUrl: './monitoring-page.component.scss'
})
export class MonitoringPageComponent {
  private readonly formBuilder = inject(FormBuilder);

  protected readonly form = this.formBuilder.group({
    make: [''],
    model: [''],
    yearFrom: [null as number | null],
    yearTo: [null as number | null],
    uf: ['']
  });

  protected readonly preview = signal<string>('No draft submitted yet.');

  protected previewDraft(): void {
    const value = this.form.getRawValue();
    this.preview.set(JSON.stringify(value));
  }
}
