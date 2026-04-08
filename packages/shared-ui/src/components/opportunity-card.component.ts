import { CommonModule, CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Component, Input } from '@angular/core';
import { isValidLotUrl, toLotStatusLabel, toOpportunityLabel, toRiskDecisionLabel } from '@leilaoauto/shared-core';

export interface OpportunityCardViewModel {
  title: string;
  source: string;
  location: string;
  value: number;
  status: number;
  score: number;
  scoreLabel: string;
  riskScore: number;
  riskDecision: string;
  summary?: string;
  lotUrl?: string;
  dateUtc?: string;
}

@Component({
  selector: 'la-opportunity-card',
  standalone: true,
  imports: [CommonModule, CurrencyPipe, DecimalPipe, DatePipe],
  template: `
    <article class="lot-card">
      <h4>{{ item.title }}</h4>
      <p><strong>Fonte:</strong> {{ item.source }}</p>
      <p><strong>Local:</strong> {{ item.location }}</p>
      <p><strong>Valor:</strong> {{ item.value | currency: 'BRL' : 'symbol' : '1.2-2' }}</p>
      <p><strong>Status:</strong> {{ lotStatus(item.status) }}</p>
      <p><strong>Oportunidade:</strong> {{ opportunity(item.scoreLabel) }} ({{ item.score | number: '1.0-2' }})</p>
      <p><strong>Risco:</strong> {{ risk(item.riskDecision) }} ({{ item.riskScore | number: '1.0-2' }})</p>
      @if (item.summary) {
        <p class="summary">{{ item.summary }}</p>
      }
      @if (item.dateUtc) {
        <small>{{ item.dateUtc | date: 'dd/MM/yyyy HH:mm' }}</small>
      }
      @if (showOpenButton()) {
        <a [href]="item.lotUrl" target="_blank" rel="noopener noreferrer">Abrir lote</a>
      }
    </article>
  `,
  styles: [
    `
      .lot-card {
        background: #121d2b;
        border: 1px solid #2a3d55;
        border-radius: 14px;
        padding: 0.86rem;
      }

      h4 {
        margin: 0 0 0.55rem;
      }

      p {
        margin: 0.24rem 0;
        color: #c2d2e0;
      }

      .summary {
        margin-top: 0.45rem;
        color: #95acc2;
      }

      small {
        display: block;
        color: #89a3bb;
        margin-top: 0.45rem;
      }

      a {
        display: inline-block;
        margin-top: 0.6rem;
        text-decoration: none;
        color: #74d5ff;
        font-weight: 700;
      }
    `
  ]
})
export class OpportunityCardComponent {
  @Input({ required: true }) item!: OpportunityCardViewModel;

  protected lotStatus(status: number): string {
    return toLotStatusLabel(status);
  }

  protected opportunity(label: string): string {
    return toOpportunityLabel(label);
  }

  protected risk(decision: string): string {
    return toRiskDecisionLabel(decision);
  }

  protected showOpenButton(): boolean {
    return isValidLotUrl(this.item.lotUrl);
  }
}
