export interface BillingPlanDetails {
  plan: number;
  displayName: string;
  monthlyPrice: number;
  features: string[];
}

export interface BillingPlanResponse {
  userId: string;
  currentPlan: number;
  currentPlanDisplayName: string;
  subscriptionStatus: number | null;
  subscriptionEndsAt: string | null;
  plans: BillingPlanDetails[];
}

export interface BillingCheckoutRequest {
  targetPlan: number;
  successUrl?: string | null;
  cancelUrl?: string | null;
}

export interface BillingCheckoutResponse {
  provider: string;
  sessionId: string;
  checkoutUrl: string;
  targetPlan: number;
  expiresAtUtc: string;
  message: string;
}
