import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AccountService } from '../../core/services/account.service';
import { AuthService } from '../../core/services/auth.service';
import { MarketService } from '../../core/services/market.service';
import { Account } from '../../core/models/account.model';
import { MarketRate } from '../../core/models/market.model';
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
  marketRates: MarketRate[] = [];
  marketUpdateTime = '';
  isLoading = true;
  errorMessage = '';

  constructor(
    private accountService: AccountService,
    private authService: AuthService,
    private marketService: MarketService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadAccounts();
    this.loadMarketRates();
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

  loadMarketRates(): void {
    // Öne çıkan kurlar: USD, EUR, GBP + gram altın
    this.marketService.getCurrencyRates().subscribe({
      next: (currencies) => {
        const featured = ['USD', 'EUR', 'GBP'];
        const featuredCurrencies = currencies.filter(c => featured.includes(c.code));

        this.marketService.getGoldPrices().subscribe({
          next: (gold) => {
            const gram = gold.find(g => g.name.includes('1Gr'));
            this.marketRates = gram
              ? [...featuredCurrencies, { ...gram, code: 'ALTIN', name: 'Gram Altın' }]
              : featuredCurrencies;

            if (this.marketRates.length > 0) {
              this.marketUpdateTime = this.marketRates[0].rateDate;
            }
          },
          error: () => {
            this.marketRates = featuredCurrencies;
          }
        });
      },
      error: () => {
        // Kur şeridi yüklenemezse dashboard'un geri kalanı etkilenmesin — sessizce gizlenir
        this.marketRates = [];
      }
    });
  }

  logout(): void {
    this.authService.logout();
  }

  getTotalBalance(): number {
    return this.accounts
      .filter(a => a.currency === 'TL')
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