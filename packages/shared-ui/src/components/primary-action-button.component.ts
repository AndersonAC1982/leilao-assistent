import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'la-primary-action-button',
  standalone: true,
  template: `
    <button type="button" [disabled]="disabled" (click)="action.emit()">
      {{ label }}
    </button>
  `,
  styles: [
    `
      button {
        border: none;
        border-radius: 10px;
        padding: 0.56rem 0.85rem;
        font-weight: 700;
        cursor: pointer;
        color: #061018;
        background: linear-gradient(90deg, #4de8a9, #37d39f);
      }

      button:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
    `
  ]
})
export class PrimaryActionButtonComponent {
  @Input() label = 'Acao';
  @Input() disabled = false;

  @Output() action = new EventEmitter<void>();
}
