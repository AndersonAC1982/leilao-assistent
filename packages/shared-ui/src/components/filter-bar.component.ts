import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'la-filter-bar',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <form class="filters" [formGroup]="form" (ngSubmit)="apply.emit()">
      <input formControlName="search" placeholder="Busca geral" />
      <input formControlName="source" placeholder="Fonte" />
      <input type="number" formControlName="minScore" placeholder="Score minimo" />
      <input type="number" formControlName="vehicleType" placeholder="Tipo" />
      <input formControlName="region" placeholder="Regiao/UF" />
      <button type="submit">Aplicar</button>
      <button type="button" class="secondary" (click)="reset.emit()">Limpar</button>
    </form>
  `,
  styles: [
    `
      .filters {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(130px, 1fr));
        gap: 0.45rem;
      }

      input {
        border-radius: 10px;
        border: 1px solid #2f445d;
        background: #0f1824;
        color: #e2edf6;
        padding: 0.5rem 0.64rem;
      }

      button {
        border: none;
        border-radius: 10px;
        padding: 0.5rem;
        font-weight: 700;
        cursor: pointer;
        color: #061018;
        background: linear-gradient(90deg, #4de8a9, #37d39f);
      }

      .secondary {
        color: #dce8f3;
        background: #24374c;
      }
    `
  ]
})
export class FilterBarComponent {
  @Input({ required: true }) form!: FormGroup;

  @Output() apply = new EventEmitter<void>();
  @Output() reset = new EventEmitter<void>();
}
