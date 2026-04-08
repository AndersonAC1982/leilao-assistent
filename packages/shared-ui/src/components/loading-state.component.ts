import { Component, Input } from '@angular/core';

@Component({
  selector: 'la-loading-state',
  standalone: true,
  template: `<p class="loading">{{ message }}</p>`,
  styles: [
    `
      .loading {
        margin: 0;
        color: #9db5cc;
        font-weight: 600;
      }
    `
  ]
})
export class LoadingStateComponent {
  @Input() message = 'Carregando...';
}
