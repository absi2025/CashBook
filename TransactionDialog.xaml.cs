using System;
using System.Windows;
using System.Windows.Controls;
using CashBook.Data;
using CashBook.Models;

namespace CashBook
{
    public partial class TransactionDialog : Window
    {
        private readonly Database _db;
        private readonly Transaction? _existing;

        public TransactionDialog(Database db, Transaction? existing = null)
        {
            InitializeComponent();
            _db = db;
            _existing = existing;

            if (existing != null)
            {
                Title = "تعديل حركة";
                DatePicker.SelectedDate = existing.Date;
                TypeCombo.SelectedIndex = (int)existing.Type;
                AccountCombo.SelectedIndex = (int)existing.Account;
                if (existing.ToAccount.HasValue)
                    ToAccountCombo.SelectedIndex = (int)existing.ToAccount.Value;
                AmountText.Text = existing.Amount.ToString("0.##");
                PartyText.Text = existing.Party;
                CategoryText.Text = existing.Category;
                NoteText.Text = existing.Note;
            }
            else
            {
                Title = "إضافة حركة";
                DatePicker.SelectedDate = DateTime.Today;
                TypeCombo.SelectedIndex = 0;
                AccountCombo.SelectedIndex = 0;
                ToAccountCombo.SelectedIndex = 1;
            }
            UpdateVisibility();
        }

        private void TypeCombo_Changed(object sender, SelectionChangedEventArgs e) => UpdateVisibility();

        private void UpdateVisibility()
        {
            if (TypeCombo.SelectedIndex == 2)
            {
                ToAccountLabel.Visibility = Visibility.Visible;
                ToAccountCombo.Visibility = Visibility.Visible;
            }
            else
            {
                ToAccountLabel.Visibility = Visibility.Collapsed;
                ToAccountCombo.Visibility = Visibility.Collapsed;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!DatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("الرجاء تحديد التاريخ");
                return;
            }
            if (!decimal.TryParse(AmountText.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("الرجاء إدخال مبلغ صحيح أكبر من صفر");
                return;
            }
            if (TypeCombo.SelectedIndex == 2 &&
                AccountCombo.SelectedIndex == ToAccountCombo.SelectedIndex)
            {
                MessageBox.Show("لا يمكن التحويل لنفس الحساب");
                return;
            }

            var t = _existing ?? new Transaction();
            t.Date = DatePicker.SelectedDate.Value;
            t.Type = (TransactionType)TypeCombo.SelectedIndex;
            t.Account = (AccountType)AccountCombo.SelectedIndex;
            t.ToAccount = t.Type == TransactionType.Transfer
                ? (AccountType)ToAccountCombo.SelectedIndex : null;
            t.Amount = amount;
            t.Party = PartyText.Text.Trim();
            t.Category = CategoryText.Text.Trim();
            t.Note = NoteText.Text.Trim();

            if (_existing == null) _db.AddTransaction(t);
            else _db.UpdateTransaction(t);

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
