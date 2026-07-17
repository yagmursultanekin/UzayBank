import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AccountService } from '../../core/services/account.service';
import { AuthService } from '../../core/services/auth.service';
import { MarketService } from '../../core/services/market.service';
import { Account } from '../../core/models/account.model';
import { MarketRate } from '../../core/models/market.model';
import { LanguageSwitcherComponent } from '../../shared/language-switcher/language-switcher';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, LanguageSwitcherComponent, TranslatePipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class DashboardComponent implements OnInit {
  accounts: Account[] = [];
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
    // VakıfBank para birimini "TL" olarak dönüyor ("TRY" değil)
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
  
  get isAdmin(): boolean {
    return this.authService.isAdmin();
  }

  goToAdmin(): void {
    this.router.navigate(['/admin']);
  }
}