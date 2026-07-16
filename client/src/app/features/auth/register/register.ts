import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../../core/services/auth.service';
import { RegisterRequest } from '../../../core/models/auth.model';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, TranslatePipe],
  templateUrl: './register.html',
  styleUrl: './register.scss'
})
export class RegisterComponent {
  registerData: RegisterRequest = {
    fullName: '',
    email: '',
    password: ''
  };
  errorMessage = '';
  isLoading = false;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

onSubmit(): void {
    if (!this.registerData.email.trim().toLowerCase().endsWith('@uzaybank.com')) {
      this.errorMessage = 'AUTH.EMAIL_DOMAIN_ERROR';
      return;
    }
    this.isLoading = true;
    this.errorMessage = '';

    this.authService.register(this.registerData).subscribe({
      next: () => {
        this.router.navigate(['/login'], {
          state: { successMessage: 'AUTH.REGISTER_SUCCESS' }
        });
      },
      error: (err) => {
        this.errorMessage = this.errorKey(err);
        this.isLoading = false;
      }
    });
  }

  private errorKey(err: any): string{
    const code = err?.error?.code;
    return code ? `ERRORS.${code}` : 'COMMON.ERROR';
  }

  getPasswordStrength(): number {
    const password = this.registerData.password;
    if (!password) return 0;

    let strength = 0;

    if (password.length >= 8) strength++;
    if (/[A-Z]/.test(password)) strength++;
    if (/[a-z]/.test(password)) strength++;
    if (/[0-9]/.test(password)) strength++;
    if (/[^A-Za-z0-9]/.test(password)) strength++;

    return strength;
  }

  // Artık hazır metin değil, çeviri ANAHTARI döndürüyor — HTML'de | translate ile çevriliyor
  getStrengthLabel(): string {
    const strength = this.getPasswordStrength();
    if (strength === 0) return '';
    if (strength <= 2) return 'AUTH.STRENGTH_WEAK';
    if (strength <= 3) return 'AUTH.STRENGTH_MEDIUM';
    if (strength === 4) return 'AUTH.STRENGTH_STRONG';
    return 'AUTH.STRENGTH_VERY_STRONG';
  }

  getStrengthClass(): string {
    const strength = this.getPasswordStrength();
    if (strength <= 2) return 'weak';
    if (strength <= 3) return 'medium';
    if (strength === 4) return 'strong';
    return 'very strong';
  }
}