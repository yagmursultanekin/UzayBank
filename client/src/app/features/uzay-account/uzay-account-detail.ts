import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { UzayAccountService } from '../../core/services/uzay-account.service';
import { Account } from '../../core/models/account.model';
import { Transaction, TransactionType } from '../../core/models/transaction.model';

@Component({
  selector: 'app-uzay-account-detail',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './uzay-account-detail.html',
  styleUrl: './uzay-account-detail.scss'
})
export class UzayAccountDetailComponent implements OnInit {
  private uzayAccountService = inject(UzayAccountService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  account: Account | null = null;
  transactions: Transaction[] = [];
  isLoading = true;

  TransactionType = TransactionType;

  ngOnInit(): void {
    const accountId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadAccount(accountId);
    this.loadTransactions(accountId);
  }

  /**
   * Hesap bilgisi için ayrı endpoint yok — kullanıcının hesapları arasından
   * ilgili olanı seçiyoruz. Liste zaten küçük (kullanıcının kendi hesapları).
   */
  loadAccount(accountId: number): void {
    this.uzayAccountService.getMyAccounts().subscribe({
      next: (accounts) => {
        this.account = accounts.find(a => a.id === accountId) ?? null;
      }
    });
  }

  loadTransactions(accountId: number): void {
    this.isLoading = true;
    this.uzayAccountService.getTransactions(accountId).subscribe({
      next: (data) => {
        this.transactions = data;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }
}