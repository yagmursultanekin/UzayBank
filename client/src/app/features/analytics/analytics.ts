import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AccountService } from '../../core/services/account.service';
import { Transaction } from '../../core/models/transaction.model';
import { SpendingChartComponent } from '../dashboard/spending-chart/spending-chart';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-analytics',
  standalone: true,
  imports: [CommonModule, SpendingChartComponent, TranslatePipe],
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
    this.loadTransactions();
  }

  /**
   * Kullanıcının TÜM hesaplarındaki son 30 günlük işlemleri çeker.
   *
   * Önceden tek bir hesap (id=2) sabit yazılmıştı; o hesapta hiç işlem
   * olmadığı için analiz hep boş çıkıyordu. Artık backend tüm hesapları
   * dolaşıp birleştiriyor.
   */
  loadTransactions(): void {
    const today = new Date();
    const thirtyDaysAgo = new Date();
    thirtyDaysAgo.setDate(today.getDate() - 30);

    const start = thirtyDaysAgo.toISOString().split('T')[0];
    const end = today.toISOString().split('T')[0];

    this.accountService.getAllTransactions(start, end).subscribe({
      next: (data) => {
        this.transactions = data;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'COMMON.ERROR';
        this.isLoading = false;
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }
}