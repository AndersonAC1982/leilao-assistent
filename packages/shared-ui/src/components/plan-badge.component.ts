import { Component, Input } from '@angular/core';
import type { PlanType } from '@leilaoauto/shared-types';
import { toPlanLabel } from '@leilaoauto/shared-core';

@Component({
  selector: 'la-plan-badge',
  standalone: true,
  template: `<span class="badge" [attr.data-plan]="plan">{{ label() }}</span>`,
  styles: [
    `
      .badge {
        display: inline-block;
        border-radius: 999px;
        border: 1px solid #375273;
        padding: 0.22rem 0.58rem;
        font-size: 0.75rem;
        font-weight: 700;
        background: #152233;
        color: #dce8f3;
      }

      .badge[data-plan='4'] {
        border-color: #a97f32;
      }
    `
  ]
})
export class PlanBadgeComponent {
  @Input() plan: PlanType | number = 1;

  protected label(): string {
    return toPlanLabel(this.plan);
  }
}
