import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  constructor(private readonly authService: AuthService) {
    this.authService.bootstrapUser().subscribe({
      error: () => this.authService.logout()
    });
  }
}
