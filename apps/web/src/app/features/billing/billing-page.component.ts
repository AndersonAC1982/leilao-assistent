import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { finalize } from 'rxjs';
import type { BillingPlanDetails, BillingPlanResponse } from '@leilaoauto/shared-types';
import { LeilaoApiService } from '@leilaoauto/shared-services';

@Component({
  selector: 'app-billing-page',
  imports: [CommonModule, CurrencyPipe, DatePipe],
  templateUrl: './billing-page.component.html',
  styleUrl: './billing-page.component.scss'
})
export class BillingPageComponent implements OnInit {
  protected readonly loading = signal(false);
  protected readonly checkoutLoadingPlan = signal<number | null>(null);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly infoMessage = signal<string | null>(null);
  protected readonly data = signal<BillingPlanResponse | null>(null);

  protected readonly currentPlanCard = computed(() => {
    const payload = this.data();
    if (!payload) {
      return null;
    }

    return payload.plans.find((plan) => plan.plan === payload.currentPlan) ?? null;
  });

  constructor(private readonly apiService: LeilaoApiService) {}

  ngOnInit(): void {
    this.loadPlan();
  }

  protected isCurrent(plan: BillingPlanDetails): boolean {
    return this.data()?.currentPlan === plan.plan;
  }

  protected canUpgrade(plan: BillingPlanDetails): boolean {
    const currentPlan = this.data()?.currentPlan ?? 1;
    return plan.plan > currentPlan;
  }

  protected trackByPlan(_index: number, plan: BillingPlanDetails): number {
    return plan.plan;
  }

  protected checkout(plan: BillingPlanDetails): void {
    if (!this.canUpgrade(plan)) {
      return;
    }

    this.errorMessage.set(null);
    this.infoMessage.set(null);
    this.checkoutLoadingPlan.set(plan.plan);

    this.apiService
      .checkoutBilling({
        targetPlan: plan.plan,
        successUrl: `${window.location.origin}/billing?checkout=success`,
        cancelUrl: `${window.location.origin}/billing?checkout=cancel`
      })
      .pipe(finalize(() => this.checkoutLoadingPlan.set(null)))
      .subscribe({
        next: (response) => {
          this.infoMessage.set(`Checkout criado com sucesso via ${response.provider}.`);
          if (response.checkoutUrl) {
            window.open(response.checkoutUrl, '_blank', 'noopener');
          }
        },
        error: (error) => {
          this.errorMessage.set(error?.error?.detail ?? 'Não foi possível iniciar o checkout.');
        }
      });
  }

  protected localizedPlanName(name: string | null | undefined): string {
    if (!name) {
      return 'Grátis';
    }

    const normalized = name.trim().toLowerCase();
    switch (normalized) {
      case 'free':
      case 'gratis':
      case 'grátis':
        return 'Grátis';
      case 'pro':
        return 'Pro';
      case 'premium':
        return 'Premium';
      case 'elite':
        return 'Elite';
      default:
        return name;
    }
  }

  private loadPlan(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.apiService
      .getBillingPlan()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => this.data.set(response),
        error: (error) => {
          this.data.set(null);
          this.errorMessage.set(error?.error?.detail ?? 'Não foi possível carregar o plano atual.');
        }
      });
  }
}


