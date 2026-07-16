import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../../core/services/auth.service';
import { LoginRequest } from '../../../core/models/auth.model';
import { LanguageSwitcherComponent } from '../../../shared/language-switcher/language-switcher';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, TranslatePipe, LanguageSwitcherComponent],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class LoginComponent {
  loginData: LoginRequest = {
    email: '',
    password: ''
  };

  // Bu alanlar artık hazır METİN değil, çeviri ANAHTARI tutuyor.
  // HTML tarafında | translate pipe'ından geçiriliyorlar.
  errorMessage = '';
  successMessage = '';

  messageType: 'success' | 'warning' = 'success';
  isLoading = false;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {
    const nav = this.router.getCurrentNavigation();
    this.successMessage = nav?.extras?.state?.['successMessage'] ?? '';
    this.messageType = nav?.extras?.state?.['messageType'] ?? 'success';
  }

  onSubmit(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.authService.login(this.loginData).subscribe({
      next: (response) => {
        this.authService.saveToken(response.token);
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.errorMessage = this.errorKey(err);
        this.isLoading = false;
      }
    });
  }
    private errorKey(err: any): string {
    const code = err?.error?.code;
    return code ? `ERRORS.${code}` : 'COMMON.ERROR';
  }
}