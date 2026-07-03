export interface Account{
    id: number;
    accountNumber: string;
    iban: string;
    currency: string; 
    balance: number;
    accountHolderName: string;
}