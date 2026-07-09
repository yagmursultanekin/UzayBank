import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const token = authService.getToken();

  const request = token
    ? req.clone({
        headers: req.headers.set('Authorization', `Bearer ${token}`)
      })
    : req;

  return next(request).pipe(
    catchError((error: HttpErrorResponse) => {
      // Oturum süresi dolduysa (401) token'ı temizle ve login'e yönlendir.
      // Login/register isteklerinin kendi 401/400'leri bu kapsamda değil —
      // onlar component'lerde "şifre hatalı" gibi mesajlarla ele alınıyor.
      if (error.status === 401 && !req.url.includes('/api/auth/')) {
        authService.logout();
        router.navigate(['/login'], {
          state: { successMessage: 'Oturum süreniz doldu. Lütfen tekrar giriş yapın.' ,
            messageType: 'warning'
          }
        });
      }
      return throwError(() => error);
    })
  );
};