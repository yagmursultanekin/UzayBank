import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Account } from '../models/account.model';
import { Transaction, CreateTransactionRequest } from '../models/transaction.model';

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  // private apiUrl = 'https://localhost:7243/api/account';
    private apiUrl = 'https://localhost:7100/api/account';


  constructor(private http: HttpClient) {}

  getMyAccounts(): Observable<Account[]> {
    return this.http.get<Account[]>(`${this.apiUrl}/my-accounts`);
  }

  getAccountById(accountId: number): Observable<Account> {
    return this.http.get<Account>(`${this.apiUrl}/${accountId}`);
  }

  getTransactions(accountId: number, startDate: string, endDate: string): Observable<Transaction[]> {
    return this.http.get<Transaction[]>(
      `${this.apiUrl}/${accountId}/transactions?startDate=${startDate}&endDate=${endDate}`
    );
  }

  addTransaction(accountId: number, request: CreateTransactionRequest): Observable<Transaction> {
    return this.http.post<Transaction>(`${this.apiUrl}/${accountId}/transactions`, request);
  }
}