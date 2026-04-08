import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'la-app-header',
  standalone: true,
  imports: [CommonModule],
  template: `
    <header class="header-shell">
      <div>
        <h2>{{ title }}</h2>
        <p>{{ subtitle }}</p>
      </div>
      <button
        type="button"
        class="primary"
        [disabled]="actionDisabled"
        (click)="action.emit()">
        {{ actionLabel }}
      </button>
    </header>
  `,
  styles: [
    `
      .header-shell {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 0.9rem;
        padding: 0.9rem;
        border: 1px solid #2a3d55;
        border-radius: 14px;
        background: linear-gradient(145deg, #121c28 0%, #1b2a3a 100%);
      }

      h2 {
        margin: 0;
        font-size: 1.08rem;
      }

      p {
        margin: 0.24rem 0 0;
        color: #9db5cc;
        font-size: 0.92rem;
      }

      .primary {
        border: none;
        border-radius: 10px;
        padding: 0.56rem 0.8rem;
        font-weight: 700;
        cursor: pointer;
        color: #061018;
        background: linear-gradient(90deg, #4de8a9, #37d39f);
      }

      .primary:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
    `
  ]
})
export class AppHeaderComponent {
  @Input() title = 'LEILAOAUTO';
  @Input() subtitle = '';
  @Input() actionLabel = 'Rodar agora';
  @Input() actionDisabled = false;

  @Output() action = new EventEmitter<void>();
}
