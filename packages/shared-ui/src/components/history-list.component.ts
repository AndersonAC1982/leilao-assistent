import { CommonModule, DatePipe } from '@angular/common';
import { Component, Input } from '@angular/core';
import type { HistoryItem } from '@leilaoauto/shared-types';

@Component({
  selector: 'la-history-list',
  standalone: true,
  imports: [CommonModule, DatePipe],
  template: `
    <ul class="history">
      <li *ngFor="let item of items">
        <strong>{{ item.source }}</strong>
        <span>{{ item.executedAtUtc | date: 'dd/MM HH:mm' }}</span>
        <span>{{ item.status }}</span>
        <small>Novos lotes: {{ item.newLots }}</small>
      </li>
    </ul>
  `,
  styles: [
    `
      .history {
        list-style: none;
        margin: 0;
        padding: 0;
        display: grid;
        gap: 0.45rem;
      }

      li {
        display: grid;
        gap: 0.15rem;
        border: 1px solid #2a3d55;
        border-radius: 10px;
        padding: 0.6rem;
        background: #111a26;
      }

      strong {
        font-size: 0.92rem;
      }

      span,
      small {
        color: #9db5cc;
      }
    `
  ]
})
export class HistoryListComponent {
  @Input() items: HistoryItem[] = [];
}
