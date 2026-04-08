import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { LeilaoApiService } from '@leilaoauto/shared-services';
import type { HistoryItem } from '@leilaoauto/shared-types';
import { EmptyStateComponent, HistoryListComponent, LoadingStateComponent } from '@leilaoauto/shared-ui';
import { finalize } from 'rxjs/operators';

@Component({
  selector: 'app-history-page',
  standalone: true,
  imports: [CommonModule, HistoryListComponent, EmptyStateComponent, LoadingStateComponent],
  templateUrl: './history-page.component.html',
  styleUrl: './history-page.component.scss'
})
export class HistoryPageComponent implements OnInit {
  protected readonly loading = signal(false);
  protected readonly items = signal<HistoryItem[]>([]);

  constructor(private readonly apiService: LeilaoApiService) {}

  ngOnInit(): void {
    this.load();
  }

  protected reload(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.apiService
      .getExecutionHistory(12)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response: HistoryItem[]) => this.items.set(response),
        error: () => this.items.set([])
      });
  }
}
