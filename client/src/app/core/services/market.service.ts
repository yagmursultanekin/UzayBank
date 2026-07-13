import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { MarketRate } from '../models/market.model';

@Injectable({
  providedIn: 'root'
})
export class MarketService {
  private apiUrl = 'https://localhost:7100/api/market';

  constructor(private http: HttpClient) {}

  getCurrencyRates(): Observable<MarketRate[]> {
    return this.http.get<MarketRate[]>(`${this.apiUrl}/currencies`);
  }

  getGoldPrices(): Observable<MarketRate[]> {
    return this.http.get<MarketRate[]>(`${this.apiUrl}/gold`);
  }
}