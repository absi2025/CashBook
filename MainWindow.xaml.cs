using System;
using System.IO;
using System.Linq;
using System.Windows;
using CashBook.Data;
using CashBook.Models;
using CashBook.Services;
using Microsoft.Win32;

namespace CashBook
{
    public partial class MainWindow : Window
    {
        private readonly Database _db = new();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CompanyNameText.Text = _db.GetSetting("company_name", "مؤسستي");
            CurrencyText.Text = _db.GetSetting("currency", "د.ل");
            DbPathText.Text = _db.DbPath;
            HeaderText.Text = CompanyNameText.Text;
            RefreshAll();
        }

        private void RefreshAll()
        {
            var all = _db.GetTransactions();
            var cur = CurrencyText.Text;
            CashBalanceText.Text = _db.GetBalance(AccountType.Cash).ToString("N2") + " " + cur;
            BankBalanceText.Text = _db.GetBalance(AccountType.Bank).ToString("N2") + " " + cur;

            RecentGrid.ItemsSource = all.Take(10).ToList();
            TransactionsGrid.ItemsSource = all;
            CountText.Text = $"عدد الحركات: {all.Count}";
        }

        private void AddTransaction_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new TransactionDialog(_db) { Owner = this };
            if (dlg.ShowDialog() == true) RefreshAll();
        }

        private void EditTransaction_Click(object sender, RoutedEventArgs e)
        {
            if (TransactionsGrid.SelectedItem is not Transaction t)
            {
                MessageBox.Show("الرجاء اختيار حركة للتعديل");
                return;
            }
            var dlg = new TransactionDialog(_db, t) { Owner = this };
            if (dlg.ShowDialog() == true) RefreshAll();
        }

        private void DeleteTransaction_Click(object sender, RoutedEventArgs e)
        {
            if (TransactionsGrid.SelectedItem is not Transaction t)
            {
                MessageBox.Show("الرجاء اختيار حركة للحذف");
                return;
            }
            if (MessageBox.Show($"هل تريد حذف هذه الحركة؟\n{t.TypeDisplay} — {t.Amount:N2}",
                "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _db.DeleteTransaction(t.Id);
                RefreshAll();
            }
        }

        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            TransactionType? type = FilterType.SelectedIndex switch
            {
                1 => TransactionType.Income,
                2 => TransactionType.Expense,
                3 => TransactionType.Transfer,
                _ => null
            };
            var list = _db.GetTransactions(
                FilterFrom.SelectedDate, FilterTo.SelectedDate, type, null, FilterParty.Text);
            TransactionsGrid.ItemsSource = list;
            CountText.Text = $"عدد الحركات: {list.Count}";
        }

        private void ClearFilter_Click(object sender, RoutedEventArgs e)
        {
            FilterFrom.SelectedDate = null;
            FilterTo.SelectedDate = null;
            FilterType.SelectedIndex = 0;
            FilterParty.Text = "";
            RefreshAll();
        }

        private void ShowReport_Click(object sender, RoutedEventArgs e)
        {
            var from = ReportFrom.SelectedDate ?? DateTime.Today.AddMonths(-1);
            var to = ReportTo.SelectedDate ?? DateTime.Today;
            AccountType? acc = ReportAccount.SelectedIndex switch
            {
                1 => AccountType.Cash,
                2 => AccountType.Bank,
                _ => null
            };
            ReportGrid.ItemsSource = _db.GetTransactions(from, to, null, acc);
        }

        private void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            var from = ReportFrom.SelectedDate ?? DateTime.Today.AddMonths(-1);
            var to = ReportTo.SelectedDate ?? DateTime.Today;
            AccountType? acc = ReportAccount.SelectedIndex switch
            {
                1 => AccountType.Cash,
                2 => AccountType.Bank,
                _ => null
            };
            var list = _db.GetTransactions(from, to, null, acc);
            if (list.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات للطباعة");
                return;
            }

            var sfd = new SaveFileDialog
            {
                Filter = "PDF Files|*.pdf",
                FileName = $"تقرير_{from:yyyyMMdd}_{to:yyyyMMdd}.pdf"
            };
            if (sfd.ShowDialog() != true) return;

            string accName = ReportAccount.SelectedIndex switch
            {
                1 => "الخزينة",
                2 => "المصرف",
                _ => "الخزينة والمصرف"
            };

            PdfService.GenerateReport(sfd.FileName, $"تقرير {accName}",
                CompanyNameText.Text, from, to, list, CurrencyText.Text);

            MessageBox.Show("تم إنشاء التقرير بنجاح");
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = sfd.FileName,
                UseShellExecute = true
            });
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            _db.SetSetting("company_name", CompanyNameText.Text);
            _db.SetSetting("currency", CurrencyText.Text);
            HeaderText.Text = CompanyNameText.Text;
            RefreshAll();
            StatusText.Text = "تم حفظ الإعدادات";
        }

        private void Backup_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog
            {
                Filter = "SQLite Database|*.db",
                FileName = $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.db"
            };
            if (sfd.ShowDialog() == true)
            {
                File.Copy(_db.DbPath, sfd.FileName, true);
                StatusText.Text = "تم النسخ الاحتياطي";
            }
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe",
                Path.GetDirectoryName(_db.DbPath)!);
        }
    }
}
