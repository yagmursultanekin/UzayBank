export interface Transaction {
    id: number;
    amount: number;
    type: TransactionType;
    description: string;
    transactionDate: string;
    balanceAfterTransaction: number;
}

export enum TransactionType{
    Credit = 1,
    Debit= 2
}

export interface CreateTransactionRequest{
    amount: number;
    type: TransactionType;
    description: string;
}