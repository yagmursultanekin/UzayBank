import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AccountService } from '../../../core/services/account.service';
import { Account } from '../../../core/models/account.model';
import { Transaction, TransactionType } from '../../../core/models/transaction.model';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-account-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './account-detail.html',
  styleUrl: './account-detail.scss'
})

export class AccountDetailComponent implements OnInit {
  account: Account | null = null;
  transactions: Transaction[] = [];
  isLoading = true;
  errorMessage = '';

  startDate: string = '';
  endDate: string = '';

  TransactionType = TransactionType;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private accountService: AccountService
  ) {}
   ngOnInit(): void {
    // Varsayılan tarih aralığı: son 30 gün
    const today = new Date();
    const thirtyDaysAgo = new Date();
    thirtyDaysAgo.setDate(today.getDate() - 30);

    this.endDate = today.toISOString().split('T')[0];
    this.startDate = thirtyDaysAgo.toISOString().split('T')[0];

    const accountId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadAccount(accountId);
    this.loadTransactions(accountId);
  }
   loadAccount(accountId: number): void {
    this.accountService.getAccountById(accountId).subscribe({
      next: (data) => {
        this.account = data;
      },
      error: () => {
        this.errorMessage = 'ACCOUNT.LOAD_ACCOUNT_ERROR';
      }
    });
  }
  loadTransactions(accountId: number): void {
    this.isLoading = true;
    this.accountService.getTransactions(accountId, this.startDate, this.endDate).subscribe({
      next: (data) => {
        this.transactions = data;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'ACCOUNT.LOAD_TRANSACTIONS_ERROR';
        this.isLoading = false;
      }
    });
  }
   onFilterChange(): void {
    if (this.account) {
      this.loadTransactions(this.account.id);
    }
  }

  onStartDateChange(endInput: HTMLInputElement): void {
    // Başlangıç seçilince mevcut aralıkla listeyi güncelle,
    // ardından bitiş tarihi takvimini otomatik aç
    this.onFilterChange();
    try {
      endInput.showPicker();
    } catch {
      // showPicker desteklenmeyen tarayıcılarda sessizce geç —
      // kullanıcı bitiş tarihini elle seçmeye devam edebilir
    }
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }
}