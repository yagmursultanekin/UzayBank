import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AdminService } from '../../core/services/admin.service';
import { AuthService } from '../../core/services/auth.service';
import { LanguageSwitcherComponent } from '../../shared/language-switcher/language-switcher';
import { AccountAssignment, UserListItem } from '../../core/models/admin.model';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe, LanguageSwitcherComponent],
  templateUrl: './admin.html',
  styleUrl: './admin.scss'
})
export class AdminComponent implements OnInit {
  private adminService = inject(AdminService);
  private authService = inject(AuthService);
  private router = inject(Router);

  assignments: AccountAssignment[] = [];
  users: UserListItem[] = [];
  isLoading = true;
  message = '';          // çeviri anahtarı tutar
  messageType: 'success' | 'error' = 'success';

  // Her hesap için seçili kullanıcıyı tutar (accountNumber → userId)
  selectedUser: { [accountNumber: string]: number } = {};

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading = true;
    this.adminService.getAssignments().subscribe({
      next: (data) => {
        this.assignments = data;
        this.isLoading = false;
      },
      error: () => {
        this.showMessage('COMMON.ERROR', 'error');
        this.isLoading = false;
      }
    });

    this.adminService.getUsers().subscribe({
      next: (data) => {
        // Sadece normal kullanıcılar hesap alabilir — admin'leri listeden çıkar
        this.users = data.filter(u => u.role === 'Customer');
      }
    });
  }

  assign(accountNumber: string): void {
    const userId = this.selectedUser[accountNumber];
    if (!userId) {
      this.showMessage('ADMIN.SELECT_USER_FIRST', 'error');
      return;
    }

    this.adminService.assign(userId, accountNumber).subscribe({
      next: () => {
        this.showMessage('ADMIN.ASSIGN_SUCCESS', 'success');
        this.loadData();   // listeyi tazele
      },
      error: (err) => {
        // Backend kod dönüyor: ACCOUNT_ALREADY_ASSIGNED vb.
        const code = err?.error?.code;
        this.showMessage(code ? `ERRORS.${code}` : 'COMMON.ERROR', 'error');
      }
    });
  }

  unassign(accountNumber: string): void {
    this.adminService.unassign(accountNumber).subscribe({
      next: () => {
        this.showMessage('ADMIN.UNASSIGN_SUCCESS', 'success');
        this.loadData();
      },
      error: () => this.showMessage('COMMON.ERROR', 'error')
    });
  }

  private showMessage(key: string, type: 'success' | 'error'): void {
    this.message = key;
    this.messageType = type;
  }

  logout(): void {
    this.authService.logout();
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }
}