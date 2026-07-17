import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, finalize } from 'rxjs/operators';
import { Router } from '@angular/router';
import { LoginRequest, RegisterRequest, AuthResponse } from '../models/auth.model';

@Injectable({
    providedIn: 'root'
})
export class AuthService {
    private apiUrl = 'https://localhost:7100/api/auth';

    constructor(
        private http: HttpClient,
        private router: Router
    ) {}

    login(request: LoginRequest): Observable<AuthResponse> {
        return this.http.post<AuthResponse>(`${this.apiUrl}/login`, request);
    }

    register(request: RegisterRequest): Observable<AuthResponse> {
        return this.http.post<AuthResponse>(`${this.apiUrl}/register`, request);
    }

    saveToken(token: string): void {
        localStorage.setItem('token', token);
    }

    getToken(): string | null {
        return localStorage.getItem('token');
    }

    isLoggedIn(): boolean {
        return !!this.getToken();
    }

    /**
     * Güvenli çıkış.
     * Backend'e haber verir (token kara listeye alınır), sonra yerel oturumu temizler.
     * Backend'e ulaşılamasa bile yerel temizlik yapılır — kullanıcı her koşulda çıkabilmeli.
     */
    logout(): void {
        this.http.post(`${this.apiUrl}/logout`, {})
            .pipe(
                // Backend çökse, ağ kopsa, token süresi dolmuş olsa bile hata yutulur.
                // Amaç: çıkış işlemi hiçbir koşulda engellenmemeli.
                catchError(() => of(null)),
                // Başarı ya da hata — her iki durumda da yerel temizlik çalışır.
                finalize(() => this.clearSession())
            )
            .subscribe();
    }

    /**
     * Oturumun yerel izlerini siler ve giriş sayfasına yönlendirir.
     * 401 interceptor'ı da bunu çağırır (token süresi dolduğunda).
     */
    clearSession(): void {
        localStorage.removeItem('token');
        this.router.navigate(['/login']);
    }

    isAdmin(): boolean {
  const token = this.getToken();
  if (!token) return false;

  try {
    // JWT üç parçalı: header.payload.signature — ortadaki payload'ı çöz
    const payload = JSON.parse(atob(token.split('.')[1]));

    // Rol claim'i — ASP.NET bunu uzun bir URI anahtarıyla yazar
    const role = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
                 || payload['role'];

    return role === 'Admin';
  } catch {
    return false;
  }
}
}