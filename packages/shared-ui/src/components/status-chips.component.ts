import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

export interface StatusChip {
  label: string;
  value: string;
  tone?: 'neutral' | 'good' | 'warn' | 'danger';
}

@Component({
  selector: 'la-status-chips',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="chips">
      <article *ngFor="let chip of chips" [class]="'chip ' + (chip.tone || 'neutral')">
        <span>{{ chip.label }}</span>
        <strong>{{ chip.value }}</strong>
      </article>
    </div>
  `,
  styles: [
    `
      .chips {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
        gap: 0.55rem;
      }

      .chip {
        border-radius: 12px;
        border: 1px solid #2a3d55;
        padding: 0.6rem 0.7rem;
        background: #152232;
      }

      .chip span {
        display: block;
        color: #95acc2;
        font-size: 0.8rem;
      }

      .chip strong {
        font-size: 0.96rem;
      }

      .chip.good {
        border-color: #2c8f65;
      }

      .chip.warn {
        border-color: #8f7a2c;
      }

      .chip.danger {
        border-color: #8f3b2c;
      }
    `
  ]
})
export class StatusChipsComponent {
  @Input() chips: StatusChip[] = [];
}
