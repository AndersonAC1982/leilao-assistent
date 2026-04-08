import { Component, Input } from '@angular/core';

@Component({
  selector: 'la-empty-state',
  standalone: true,
  template: `
    <article class="empty">
      <h4>{{ title }}</h4>
      <p>{{ message }}</p>
    </article>
  `,
  styles: [
    `
      .empty {
        border: 1px dashed #33506c;
        border-radius: 12px;
        padding: 0.85rem;
        background: #101b27;
      }

      h4 {
        margin: 0 0 0.4rem;
      }

      p {
        margin: 0;
        color: #9db5cc;
      }
    `
  ]
})
export class EmptyStateComponent {
  @Input() title = 'Sem resultados';
  @Input() message = 'Nada para exibir no momento.';
}
