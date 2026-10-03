using System;
using System.Collections.Generic;
using System.IO;
using CashBook.Models;
using Microsoft.Data.Sqlite;

namespace CashBook.Data
{
    public class Database
    {
        private readonly string _dbPath;

        public Database()
        {
            _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data.db");
            Initialize();
        }

        public string DbPath => _dbPath;
        private string Cs => $"Data Source={_dbPath}";

        private void Initialize()
        {
            using var conn = new SqliteConnection(Cs);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS transactions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    date TEXT NOT NULL,
                    type INTEGER NOT NULL,
                    account INTEGER NOT NULL,
                    to_account INTEGER NULL,
                    amount REAL NOT NULL,
                    party TEXT NOT NULL DEFAULT '',
                    category TEXT NOT NULL DEFAULT '',
                    note TEXT NOT NULL DEFAULT '',
                    ref_no TEXT NOT NULL DEFAULT '',
                    created_at TEXT NOT NULL
                );
                CREATE TABLE IF NOT EXISTS settings (
                    key TEXT PRIMARY KEY,
                    value TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        public void AddTransaction(Transaction t)
        {
            using var conn = new SqliteConnection(Cs);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO transactions 
                (date,type,account,to_account,amount,party,category,note,ref_no,created_at)
                VALUES ($d,$t,$a,$ta,$am,$p,$c,$n,$r,$ca);";
            BindAll(cmd, t);
            cmd.Parameters.AddWithValue("$ca", t.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
        }

        public void UpdateTransaction(Transaction t)
        {
            using var conn = new SqliteConnection(Cs);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE transactions SET
                date=$d,type=$t,account=$a,to_account=$ta,amount=$am,
                party=$p,category=$c,note=$n,ref_no=$r
                WHERE id=$id;";
            BindAll(cmd, t);
            cmd.Parameters.AddWithValue("$id", t.Id);
            cmd.ExecuteNonQuery();
        }

        private static void BindAll(SqliteCommand cmd, Transaction t)
        {
            cmd.Parameters.AddWithValue("$d", t.Date.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$t", (int)t.Type);
            cmd.Parameters.AddWithValue("$a", (int)t.Account);
            cmd.Parameters.AddWithValue("$ta", t.ToAccount.HasValue ? (object)(int)t.ToAccount.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("$am", (double)t.Amount);
            cmd.Parameters.AddWithValue("$p", t.Party ?? "");
            cmd.Parameters.AddWithValue("$c", t.Category ?? "");
            cmd.Parameters.AddWithValue("$n", t.Note ?? "");
            cmd.Parameters.AddWithValue("$r", t.RefNo ?? "");
        }

        public void DeleteTransaction(long id)
        {
            using var conn = new SqliteConnection(Cs);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM transactions WHERE id=$id;";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }

        public List<Transaction> GetTransactions(DateTime? from = null, DateTime? to = null,
            TransactionType? type = null, AccountType? account = null, string? party = null)
        {
            var list = new List<Transaction>();
            using var conn = new SqliteConnection(Cs);
            conn.Open();
            using var cmd = conn.CreateCommand();
            var where = new List<string>();

            if (from.HasValue) { where.Add("date >= $from"); cmd.Parameters.AddWithValue("$from", from.Value.ToString("yyyy-MM-dd")); }
            if (to.HasValue)   { where.Add("date <= $to");   cmd.Parameters.AddWithValue("$to", to.Value.ToString("yyyy-MM-dd")); }
            if (type.HasValue) { where.Add("type = $type");  cmd.Parameters.AddWithValue("$type", (int)type.Value); }
            if (account.HasValue) {
                where.Add("(account = $acc OR to_account = $acc)");
                cmd.Parameters.AddWithValue("$acc", (int)account.Value);
            }
            if (!string.IsNullOrWhiteSpace(party)) {
                where.Add("party LIKE $party");
                cmd.Parameters.AddWithValue("$party", $"%{party}%");
            }

            cmd.CommandText = "SELECT * FROM transactions"
                + (where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "")
                + " ORDER BY date DESC, id DESC;";

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Transaction
                {
                    Id = r.GetInt64(0),
                    Date = DateTime.Parse(r.GetString(1)),
                    Type = (TransactionType)r.GetInt32(2),
                    Account = (AccountType)r.GetInt32(3),
                    ToAccount = r.IsDBNull(4) ? null : (AccountType)r.GetInt32(4),
                    Amount = (decimal)r.GetDouble(5),
                    Party = r.GetString(6),
                    Category = r.GetString(7),
                    Note = r.GetString(8),
                    RefNo = r.GetString(9),
                    CreatedAt = DateTime.Parse(r.GetString(10))
                });
            }
            return list;
        }

        public decimal GetBalance(AccountType account)
        {
            using var conn = new SqliteConnection(Cs);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT COALESCE(SUM(CASE 
                    WHEN type=0 AND account=$a THEN amount
                    WHEN type=1 AND account=$a THEN -amount
                    WHEN type=2 AND account=$a THEN -amount
                    WHEN type=2 AND to_account=$a THEN amount
                    ELSE 0 END),0)
                FROM transactions;";
            cmd.Parameters.AddWithValue("$a", (int)account);
            var v = cmd.ExecuteScalar();
            return Convert.ToDecimal(v);
        }

        public void SetSetting(string key, string value)
        {
            using var conn = new SqliteConnection(Cs);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO settings(key,value) VALUES($k,$v) ON CONFLICT(key) DO UPDATE SET value=$v;";
            cmd.Parameters.AddWithValue("$k", key);
            cmd.Parameters.AddWithValue("$v", value);
            cmd.ExecuteNonQuery();
        }

        public string GetSetting(string key, string def = "")
        {
            using var conn = new SqliteConnection(Cs);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT value FROM settings WHERE key=$k;";
            cmd.Parameters.AddWithValue("$k", key);
            return cmd.ExecuteScalar()?.ToString() ?? def;
        }
    }
}
