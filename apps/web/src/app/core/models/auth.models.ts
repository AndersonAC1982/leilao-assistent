export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  userId: string;
  email: string;
  token: string;
  expiresAtUtc: string;
}

export interface AuthMeResponse {
  userId: string;
  email: string;
  role: number;
  plan: number;
  createdAt: string;
}
