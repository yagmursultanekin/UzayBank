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
      // Oturum süresi dolduysa (401) yerel oturumu temizle ve login'e yönlendir.
      //
      // logout() DEĞİL, clearSession() çağrılıyor. Çünkü logout() backend'e istek atar;
      // token zaten geçersizken bu istek de 401 döner ve interceptor kendini
      // tekrar tetikler — sonsuz döngü oluşur.
      //
      // Ayrıca token zaten geçersiz olduğu için kara listeye almanın anlamı yok.
      if (error.status === 401 && !req.url.includes('/api/auth/')) {
        localStorage.removeItem('token');
        router.navigate(['/login'], {
          state: {
            successMessage: 'AUTH.SESSION_EXPIRED',
            messageType: 'warning'
          }
        });
      }
      return throwError(() => error);
    })
  );
};