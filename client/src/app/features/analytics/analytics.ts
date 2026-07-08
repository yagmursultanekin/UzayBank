import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AccountService } from '../../core/services/account.service';
import { Transaction } from '../../core/models/transaction.model';
import { SpendingChartComponent } from '../dashboard/spending-chart/spending-chart';

@Component({
  selector: 'app-analytics',
  standalone: true,
  imports: [CommonModule, SpendingChartComponent],
  templateUrl: './analytics.html',
  styleUrl: './analytics.scss'
})
export class AnalyticsComponent implements OnInit {
  transactions: Transaction[] = [];
  isLoading = true;
  errorMessage = '';

  constructor(
    private accountService: AccountService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.accountService.getAccountById(2).subscribe({
      next: (accounts) => {
        const tryAccount = accounts;
        //.find(a => a.currency === 'TRY');
        if (tryAccount) {
          this.loadTransactions(tryAccount.id);
        } else {
          this.isLoading = false;
        }
      },
      error: () => {
        this.errorMessage = 'Veriler yüklenemedi.';
        this.isLoading = false;
      }
    });
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
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'İşlemler yüklenemedi.';
        this.isLoading = false;
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }
}
