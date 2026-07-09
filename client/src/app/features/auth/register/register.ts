import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { RegisterRequest } from '../../../core/models/auth.model';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
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
    this.isLoading = true;
    this.errorMessage = '';

    this.authService.register(this.registerData).subscribe({
      next: () => {
        this.router.navigate(['/login'], {
          state: { successMessage: 'Kayıt işlemi başarılı.' }
        });
      },
      error: () => {
        this.errorMessage = 'Bu e-posta adresi zaten kayıtlı.';
        this.isLoading = false;
      }
    });
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

  getStrengthLabel(): string {
    const strength = this.getPasswordStrength();
    if (strength === 0) return '';
    if (strength <= 2) return 'Zayıf';
    if (strength <= 3) return 'Orta';
    if (strength === 4) return 'Güçlü';
    return 'Çok Güçlü';
  }

  getStrengthClass(): string {
    const strength = this.getPasswordStrength();
    if (strength <= 2) return 'weak';
    if (strength <= 3) return 'medium';
    if (strength === 4) return 'strong';
    return 'very strong';
  }
}