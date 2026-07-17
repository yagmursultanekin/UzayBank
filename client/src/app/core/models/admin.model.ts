export interface AccountAssignment {
  accountNumber: string;
  iban: string;
  currency: string;
  assignedUserId: number | null;
  assignedUserEmail: string | null;
}

export interface UserListItem {
  id: number;
  fullName: string;
  email: string;
  role: string;
}