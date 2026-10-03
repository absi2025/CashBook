using System;

namespace CashBook.Models
{
    public enum TransactionType { Income = 0, Expense = 1, Transfer = 2 }
    public enum AccountType { Cash = 0, Bank = 1 }

    public class Transaction
    {
        public long Id { get; set; }
        public DateTime Date { get; set; } = DateTime.Today;
        public TransactionType Type { get; set; }
        public AccountType Account { get; set; }
        public AccountType? ToAccount { get; set; }
        public decimal Amount { get; set; }
        public string Party { get; set; } = "";
        public string Category { get; set; } = "";
        public string Note { get; set; } = "";
        public string RefNo { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string TypeDisplay => Type switch
        {
            TransactionType.Income => "دخول",
            TransactionType.Expense => "خروج",
            TransactionType.Transfer => "تحويل",
            _ => ""
        };

        public string AccountDisplay => Account switch
        {
            AccountType.Cash => "خزينة",
            AccountType.Bank => "مصرف",
            _ => ""
        };

        public string ToAccountDisplay => ToAccount switch
        {
            AccountType.Cash => "خزينة",
            AccountType.Bank => "مصرف",
            _ => ""
        };
    }
}
