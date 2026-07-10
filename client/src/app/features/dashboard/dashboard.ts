import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AccountService } from '../../core/services/account.service';
import { AuthService } from '../../core/services/auth.service';
import { Account } from '../../core/models/account.model';
import { SpendingChartComponent } from './spending-chart/spending-chart';
import { Transaction } from '../../core/models/transaction.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, SpendingChartComponent],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class DashboardComponent implements OnInit {
  accounts: Account[] = [];
  transactions: Transaction[] = [];
  isLoading = true;
  errorMessage = '';

  constructor(
    private accountService: AccountService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadAccounts();
  }

  loadAccounts(): void {
    this.accountService.getMyAccounts().subscribe({
      next: (data) => {
  this.accounts = data;
  this.isLoading = false;

  const tryAccount = data.find(a => a.currency === 'TRY');
  if (tryAccount) {
    this.loadTransactions(tryAccount.id);
  }
},
    });
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  getTotalBalance(): number {
    return this.accounts
      .filter(a => a.currency === 'TRY')
      .reduce((sum, a) => sum + a.balance, 0);
  }

  goToAccount(accountId: number): void {
  this.router.navigate(['/accounts', accountId]);
}
goToAnalytics(): void {
  this.router.navigate(['/analytics']);
}

goToNearestAtm(): void {
    this.router.navigate(['/nearest-atm']);
  }

loadTransactions(accountId: number): void {
  const today = new Date();
  const thirtyDaysAgo = new Date();
  thirtyDaysAgo.setDate(today.getDate() - 30);

  const start = thirtyDaysAgo.toISOString().split('T')[0];
  const end = today.toISOString().split('T')[0];

  this.accountService.getTransactions(accountId, start, end).subscribe({
    next: (data) => {
      this.transactions = data;
    }
  });
}
}