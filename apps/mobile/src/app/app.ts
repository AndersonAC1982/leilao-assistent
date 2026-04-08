import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from '@leilaoauto/shared-services';

@Component({
  selector: 'app-root',
  standalone: true,
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
