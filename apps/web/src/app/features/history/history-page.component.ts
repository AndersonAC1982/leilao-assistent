import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { LeilaoApiService } from '@leilaoauto/shared-services';
import type { HistoryItem } from '@leilaoauto/shared-types';
import { EmptyStateComponent, HistoryListComponent, LoadingStateComponent } from '@leilaoauto/shared-ui';

@Component({
  selector: 'app-history-page',
  standalone: true,
  imports: [CommonModule, HistoryListComponent, EmptyStateComponent, LoadingStateComponent],
  templateUrl: './history-page.component.html',
  styleUrl: './history-page.component.scss'
})
export class HistoryPageComponent implements OnInit {
  protected readonly loading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly items = signal<HistoryItem[]>([]);

  constructor(private readonly apiService: LeilaoApiService) {}

  ngOnInit(): void {
    this.load();
  }

  protected refresh(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.apiService
      .getExecutionHistory(20)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => this.items.set(response),
        error: () => {
          this.items.set([]);
          this.errorMessage.set('Nao foi possivel carregar o historico de execucoes.');
        }
      });
  }
}
