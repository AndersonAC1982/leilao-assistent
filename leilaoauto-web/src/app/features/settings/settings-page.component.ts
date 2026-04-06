import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-settings-page',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './settings-page.component.html',
  styleUrl: './settings-page.component.scss'
})
export class SettingsPageComponent {
  private readonly formBuilder = inject(FormBuilder);

  protected readonly form = this.formBuilder.group({
    timezone: ['America/Sao_Paulo'],
    notificationEmail: [''],
    alertThreshold: [70]
  });

  protected save(): void {
    // TODO: Persist user settings in next phase.
    console.log('Settings draft', this.form.getRawValue());
  }
}
