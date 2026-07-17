import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AccountAssignment, UserListItem } from '../models/admin.model';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private apiUrl = 'https://localhost:7100/api/admin';

  constructor(private http: HttpClient) {}

  getAssignments(): Observable<AccountAssignment[]> {
    return this.http.get<AccountAssignment[]>(`${this.apiUrl}/assignments`);
  }

  getUsers(): Observable<UserListItem[]> {
    return this.http.get<UserListItem[]>(`${this.apiUrl}/users`);
  }

  assign(userId: number, accountNumber: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/assign`, { userId, accountNumber });
  }

  unassign(accountNumber: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/unassign`, { accountNumber });
  }
}