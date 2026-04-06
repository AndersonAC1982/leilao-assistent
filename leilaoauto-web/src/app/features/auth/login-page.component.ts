import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login-page',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login-page.component.html',
  styleUrl: './login-page.component.scss'
})
export class LoginPageComponent {
  private readonly formBuilder = inject(FormBuilder);

  protected readonly loading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router
  ) {
    if (this.authService.isAuthenticated()) {
      this.router.navigateByUrl('/dashboard');
    }
  }

  protected submit(mode: 'login' | 'register'): void {
    if (this.form.invalid || this.loading()) {
      this.form.markAllAsTouched();
      return;
    }

    const request = this.form.getRawValue();
    this.loading.set(true);
    this.errorMessage.set(null);

    const action = mode === 'login'
      ? this.authService.login(request)
      : this.authService.register(request);

    action.pipe(finalize(() => this.loading.set(false))).subscribe({
      next: () => this.router.navigateByUrl('/dashboard'),
      error: () => this.errorMessage.set('Authentication failed. Please verify your credentials and try again.')
    });
  }
}
