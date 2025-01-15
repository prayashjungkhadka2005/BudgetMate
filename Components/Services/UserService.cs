    using System;
    using System.Diagnostics;
    using System.IO;
    using SQLite;
    using BudgetMate.Components.Models;
    using System.Data.Common;
using static System.Runtime.InteropServices.JavaScript.JSType;

    namespace BudgetMate.Components.Services
    {
    public class UserService
    {
        private SQLiteConnection _database;
        private string _dbPath;

        private static User _currentUser = null;

        public UserService()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string folderPath = Path.Combine(desktopPath, "Test");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            _dbPath = Path.Combine(folderPath, "BudgetMateDatabase.db3");

            _database = new SQLiteConnection(_dbPath);

            _database.Execute("PRAGMA foreign_keys = ON;");

            _database.CreateTable<User>();
            _database.CreateTable<Transaction>();
            _database.CreateTable<Debit>();
            _database.CreateTable<Credit>();
            _database.CreateTable<Debt>();

            _database.CreateTable<Balance>();

            Debug.WriteLine($"Database path: {_dbPath}");
        }

        public User LoginUser(string username, string password)
        {
            var user = _database.Table<User>().FirstOrDefault(u => u.Username == username && u.Password == password);
            if (user != null)
            {
                _currentUser = user;
            }
            return user;
        }

        public void Logout()
        {
            _currentUser = null;
            Debug.WriteLine("User logged out successfully.");
        }

        public bool IsUserLoggedIn()
        {
            return _currentUser != null;
        }

        public User GetCurrentUser()
        {
            return _currentUser;
        }

        public bool RegisterUser(User user)
        {
            var existingUser = _database.Table<User>().FirstOrDefault(u => u.Username == user.Username);
            if (existingUser != null)
            {
                return false;
            }
            _database.Insert(user);
            return true;
        }





       

        public bool AddDebitTransaction(Debit debit)
        {
            if (_currentUser == null) return false;
            if (debit == null || string.IsNullOrEmpty(debit.DebitTransactionTitle) || debit.DebitAmount <= 0)
            {
                return false;
            }

            try
            {
                debit.UserId = _currentUser.UserId;
                int totalBalance = GetTotalBalance();

                if (debit.DebitAmount > totalBalance)
                {
                    return false;
                }

                _database.BeginTransaction();

                var transaction = new Transaction
                {
                    UserId = _currentUser.UserId,
                    TransactionTitle = debit.DebitTransactionTitle,
                    TransactionDate = debit.DebitTransactionDate,
                    Amount = debit.DebitAmount,
                    Type = "Debit",
                    Tags = debit.DebitTags
                };

                _database.Insert(transaction);

                // Retrieve the generated TransactionID
                int transactionId = _database.ExecuteScalar<int>("SELECT last_insert_rowid()");

                debit.TransactionID = transactionId;
                _database.Insert(debit);

                _database.Commit();
                RecalculateBalance();
                return true;
            }
            catch (SQLiteException ex)
            {
                _database.Rollback();
                Debug.WriteLine($"Error inserting debit transaction: {ex.Message}");
                return false;
            }
        }



       

        public bool AddCreditTransaction(Credit credit)
        {
            if (_currentUser == null) return false;
            if (credit == null || string.IsNullOrEmpty(credit.CreditTransactionTitle) || credit.CreditAmount <= 0)
            {
                return false;
            }

            try
            {
                credit.UserId = _currentUser.UserId;

                _database.BeginTransaction();

                var transaction = new Transaction
                {
                    UserId = _currentUser.UserId,
                    TransactionTitle = credit.CreditTransactionTitle,
                    TransactionDate = credit.CreditTransactionDate,
                    Amount = credit.CreditAmount,
                    Type = "Credit",
                    Tags = credit.CreditTags
                };

                _database.Insert(transaction);

                int transactionId = _database.ExecuteScalar<int>("SELECT last_insert_rowid()");

                credit.TransactionID = transactionId;
                _database.Insert(credit);

                _database.Commit();
                RecalculateBalance();
                return true;
            }
            catch (Exception ex)
            {
                _database.Rollback();
                Debug.WriteLine($"Error inserting credit transaction: {ex.Message}");
                return false;
            }
        }


        public bool AddDebtTransaction(Debt debt)
        {
            if (_currentUser == null) return false;
            if (debt == null || string.IsNullOrEmpty(debt.DebtTransactionTitle) || debt.DebtAmount <= 0 || debt.DebtDueDate == default)
            {
                return false;
            }

            try
            {
                debt.UserId = _currentUser.UserId;

                _database.BeginTransaction();

                var transaction = new Transaction
                {
                    UserId = _currentUser.UserId,
                    TransactionTitle = debt.DebtTransactionTitle,
                    TransactionDate = debt.DebtTransactionDate,
                    Amount = debt.DebtAmount,
                    Type = "Debt",
                    Tags = debt.SourceOfDebt
                };

                _database.Insert(transaction);

                int transactionId = _database.ExecuteScalar<int>("SELECT last_insert_rowid()");

                debt.TransactionID = transactionId;
                _database.Insert(debt);

                _database.Commit();
                RecalculateBalance();
                return true;
            }
            catch (SQLiteException ex)
            {
                _database.Rollback();
                Debug.WriteLine($"Error inserting debt transaction: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error: {ex.Message}");
                return false;
            }
        }

        public bool ClearDebt(int debtId)
        {
            try
            {
                if (_currentUser == null) return false;

                var debt = _database.Table<Debt>().FirstOrDefault(d => d.DebtId == debtId && d.UserId == _currentUser.UserId);
                if (debt != null && debt.DebtAmount < GetTotalBalance())
                {
                    _database.BeginTransaction();
                    debt.isCleared = true;
                    _database.Update(debt);
                    _database.Insert(new Transaction
                    {
                        UserId = _currentUser.UserId,
                        TransactionDate = DateTime.Now.ToString("yyyy-MM-dd"),
                        Amount = debt.DebtAmount,
                        Type = "Debt Cleared",
                        Tags = debt.SourceOfDebt,
                        Note = "Debt paid successfully"
                    });
                    _database.Commit();
                    RecalculateBalance();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _database.Rollback();
                Debug.WriteLine($"Error while clearing debt: {ex.Message}");
                return false;
            }
        }


        public List<Debt> GetPendingDebts()
        {
            if (_currentUser == null) return new List<Debt>();
            return _database.Table<Debt>().Where(debt => debt.UserId == _currentUser.UserId && !debt.isCleared).ToList();
        }




        public List<Transaction> GetAllTransactions()
            {
            if (_currentUser == null) return new List<Transaction>();
            return _database.Table<Transaction>().Where(t => t.UserId == _currentUser.UserId).OrderBy(t => t.TransactionDate).ToList();
            }


            public bool UpdateNote(int transactionId, string note)
            {
                try
                {
                if (_currentUser == null) return false;
                var transaction = _database.Table<Transaction>().FirstOrDefault(t => t.UserId == _currentUser.UserId && t.TransactionID == transactionId);
                    if (transaction != null)
                    {
                        transaction.Note = note;
                        _database.Update(transaction); // Update the transaction with the new note
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error updating note: {ex.Message}");
                    return false;
                }
            }

      

        public int GetTotalInflows()
        {
            try
            {
                if (_currentUser == null) return 0;
                int totalInflows = _database.Table<Credit>().Where(c => c.UserId == _currentUser.UserId).Sum(c => c.CreditAmount);
                int totalClearedDebts = _database.Table<Debt>().Where(d => d.UserId == _currentUser.UserId && d.isCleared).Sum(d => d.DebtAmount);
                return totalInflows - totalClearedDebts;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error calculating remaining inflow: {ex.Message}");
                return 0; 
            }
        }

        public int GetTotalOutflow()
        {
            try
            {
                if (_currentUser == null) return 0;
                return _database.Table<Debit>().Where(d => d.UserId == _currentUser.UserId).Sum(d => d.DebitAmount);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error calculating total outflows: {ex.Message}");
                return 0;
            }

        }

        public int GetTotalDebt()
        {
            try
            {
                if (_currentUser == null) return 0;
                return _database.Table<Debt>().Where(d => d.UserId == _currentUser.UserId).Sum(d => d.DebtAmount);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error calculating total debt: {ex.Message}");
                return 0;
            }

        }

        public int GetRemainingDebt()
        {
            try
            {
                if (_currentUser == null) return 0;
                return _database.Table<Debt>().Where(d => !d.isCleared && d.UserId == _currentUser.UserId).Sum(d => d.DebtAmount);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error calculating remaining debt: {ex.Message}");
                return 0; 
            }
        }

        public int GetClearedDebt()
        {
            try
            {

                if (_currentUser == null) return 0;
                return _database.Table<Debt>().Where(d => d.isCleared && d.UserId == _currentUser.UserId).Sum(d => d.DebtAmount);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error calculating cleared debt: {ex.Message}");
                return 0; 
            }
        }



        public int GetTotalNumberOfTransactions()
        {
            if (_currentUser == null) return 0;
            return _database.Table<Transaction>().Count(t => t.UserId == _currentUser.UserId);
        }


        public int GetTotalTransactionsAmount()
        {
            if (_currentUser == null) return 0;
            int totalInflow = _database.Table<Credit>().Where(c => c.UserId == _currentUser.UserId).Sum(c => c.CreditAmount);
            int totalOutflow = _database.Table<Debit>().Where(d => d.UserId == _currentUser.UserId).Sum(d => d.DebitAmount);
            int debts = _database.Table<Debt>().Where(cd => cd.UserId == _currentUser.UserId).Sum(cd => cd.DebtAmount); 

            return totalInflow + debts- totalOutflow;
        }


        public int GetTotalBalance()
        {
            var balance = _database.Table<Balance>().FirstOrDefault(b => b.UserId == _currentUser.UserId);
            return balance?.TotalBalance ?? 0; 
        }




        //public void RecalculateBalance()
        //{
        //    try
        //    {
        //        // Calculate total inflows, outflows, and remaining debts
        //        int totalInflows = GetTotalInflows();
        //        int totalOutflows = GetTotalOutflow();
        //        int remainingDebt = GetRemainingDebt();

        //        // Calculate the balance
        //        int totalBalance = totalInflows - totalOutflows - remainingDebt;

        //        var balance = _database.Table<Balance>().FirstOrDefault();
        //        if (balance == null)
        //        {
        //            balance = new Balance { TotalBalance = totalBalance };
        //            _database.Insert(balance);
        //        }
        //        else
        //        {
        //            balance.TotalBalance = totalBalance;
        //            _database.Update(balance);
        //        }


        //        Debug.WriteLine($"Recalculated Balance: {totalBalance}");
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"Error recalculating balance: {ex.Message}");
        //    }
        //}
        //public void RecalculateBalance()
        //{
        //    try
        //    {
        //        int totalInflows = GetTotalInflows();
        //        int totalOutflows = GetTotalOutflow();
        //        int remainingDebt = GetRemainingDebt();

        //        int totalBalance = totalInflows - totalOutflows - remainingDebt;

        //        var balance = _database.Table<Balance>().FirstOrDefault();
        //        if (balance == null)
        //        {
        //            _database.Insert(new Balance { TotalBalance = totalBalance });
        //        }
        //        else
        //        {
        //            _database.Execute("UPDATE Balance SET TotalBalance = ?", totalBalance);
        //        }

        //        Debug.WriteLine($"Updated Balance: {totalBalance}");
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"Error recalculating balance: {ex.Message}");
        //    }
        //}

        public void RecalculateBalance()
        {
            try
            {
                if (_currentUser == null) return;
                int totalInflows = GetTotalInflows();
                int totalOutflows = GetTotalOutflow();
                int debts = GetTotalDebt();
                int totalBalance = totalInflows - totalOutflows + debts;
                var balance = _database.Table<Balance>().FirstOrDefault(b => b.UserId == _currentUser.UserId);
                if (balance == null)

                 _database.BeginTransaction();
                if (balance == null)
                {
                    _database.Insert(new Balance { UserId = _currentUser.UserId, TotalBalance = totalBalance });
                }
                else
                {
                    balance.TotalBalance = totalBalance;
                    _database.Update(balance);
                }
                _database.Commit();
            }
            catch (Exception ex)
            {
                _database.Rollback();
                Debug.WriteLine($"Error recalculating balance: {ex.Message}");
            }
        }


        public List<Debt> GetOverdueDebts()
        {
            if (_currentUser == null) return new List<Debt>();
            var allDebts = _database.Table<Debt>().Where(debt => debt.UserId == _currentUser.UserId && !debt.isCleared).ToList();

            return allDebts
                .Where(debt => DateTime.TryParse(debt.DebtDueDate, out DateTime dueDate) && dueDate < DateTime.Now)
                .ToList();
        }

      

        public List<Debt> GetClearedDebts()
        {
            try
            {
                if (_currentUser == null) return new List<Debt>();
                return _database.Table<Debt>().Where(debt => debt.UserId == _currentUser.UserId && debt.isCleared).ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving cleared debts: {ex.Message}");
                return new List<Debt>(); // Return an empty list if there's an error
            }
        }

      


        public int GetTotalInflowsForDateRange(DateTime startDate, DateTime endDate)
        {
            if (_currentUser == null) return 0;
            try
            {

                var inflows = _database.Table<Credit>().ToList()
                    .Where(c => c.UserId == _currentUser.UserId && DateTime.TryParse(c.CreditTransactionDate, out DateTime date) && date >= startDate && date <= endDate)
                    .Sum(c => c.CreditAmount);

                var clearedDebts = _database.Table<Debt>().ToList()
                    .Where(d => d.UserId == _currentUser.UserId && d.isCleared && DateTime.TryParse(d.DebtTransactionDate, out DateTime date) && date >= startDate && date <= endDate)
                    .Sum(d => d.DebtAmount);

                return inflows - clearedDebts;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error filtering total inflows: {ex.Message}");
                return 0;
            }
        }

        public int GetTotalOutflowForDateRange(DateTime startDate, DateTime endDate)
        {
            if (_currentUser == null) return 0;

            try
            {
                return _database.Table<Debit>().ToList()
                    .Where(d => d.UserId == _currentUser.UserId && DateTime.TryParse(d.DebitTransactionDate, out DateTime date) && date >= startDate && date <= endDate)
                    .Sum(d => d.DebitAmount);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error filtering total outflows: {ex.Message}");
                return 0;
            }
        }

        public int GetTotalDebtForDateRange(DateTime startDate, DateTime endDate)
        {
            if (_currentUser == null) return 0;

            try
            {
                return _database.Table<Debt>().ToList()
                    .Where(d => d.UserId == _currentUser.UserId && DateTime.TryParse(d.DebtTransactionDate, out DateTime date) && date >= startDate && date <= endDate)
                    .Sum(d => d.DebtAmount);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error filtering total debt: {ex.Message}");
                return 0;
            }
        }

        public int GetRemainingDebtForDateRange(DateTime startDate, DateTime endDate)
        {
            if (_currentUser == null) return 0;
            try
            {
                return _database.Table<Debt>().ToList()
                    .Where(d => d.UserId == _currentUser.UserId && !d.isCleared && DateTime.TryParse(d.DebtTransactionDate, out DateTime date) && date >= startDate && date <= endDate)
                    .Sum(d => d.DebtAmount);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error filtering remaining debt: {ex.Message}");
                return 0;
            }
        }

        public int GetClearedDebtForDateRange(DateTime startDate, DateTime endDate)
        {
            if (_currentUser == null) return 0;
            try
            {
                return _database.Table<Debt>().ToList()
                    .Where(d => d.UserId == _currentUser.UserId && d.isCleared && DateTime.TryParse(d.DebtTransactionDate, out DateTime date) && date >= startDate && date <= endDate)
                    .Sum(d => d.DebtAmount);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error filtering cleared debt: {ex.Message}");
                return 0;
            }
        }

        public List<Debt> GetClearedDebtsListForDateRange(DateTime startDate, DateTime endDate)
        {
            if (_currentUser == null) return new List<Debt>();

            try

            {
                // Fetch all cleared debts
                var allClearedDebts = _database.Table<Debt>().Where(debt => debt.UserId == _currentUser.UserId && debt.isCleared).ToList();

                // Filter debts by due date range after fetching
                return allClearedDebts
                    .Where(debt => DateTime.TryParse(debt.DebtDueDate, out DateTime dueDate) &&
                                   dueDate >= startDate &&
                                   dueDate <= endDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving cleared debts for date range based on due date: {ex.Message}");
                return new List<Debt>(); // Return an empty list if there's an error
            }
        }


        public int GetTotalNumberOfTransactionsForDateRange(DateTime startDate, DateTime endDate)
        {
            if (_currentUser == null) return 0;
            try
            {
                int debitCount = _database.Table<Debit>().ToList()
                    .Count(d => d.UserId == _currentUser.UserId && DateTime.TryParse(d.DebitTransactionDate, out DateTime date) && date >= startDate && date <= endDate);

                int creditCount = _database.Table<Credit>().ToList()
                    .Count(c => c.UserId == _currentUser.UserId && DateTime.TryParse(c.CreditTransactionDate, out DateTime date) && date >= startDate && date <= endDate);

                int clearedDebtCount = _database.Table<Debt>().ToList()
                    .Count(d => d.UserId == _currentUser.UserId && d.isCleared && DateTime.TryParse(d.DebtTransactionDate, out DateTime date) && date >= startDate && date <= endDate);

                return debitCount + creditCount + clearedDebtCount;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error filtering total number of transactions: {ex.Message}");
                return 0;
            }
        }

        public int GetTotalTransactionsAmountForDateRange(DateTime startDate, DateTime endDate)
        {
            if (_currentUser == null) return 0;
            try
            {
                int totalInflow = _database.Table<Credit>().ToList()
                    .Where(c => c.UserId == _currentUser.UserId && DateTime.TryParse(c.CreditTransactionDate, out DateTime date) && date >= startDate && date <= endDate)
                    .Sum(c => c.CreditAmount);

                int totalOutflow = _database.Table<Debit>().ToList()
                    .Where(d => d.UserId == _currentUser.UserId &&  DateTime.TryParse(d.DebitTransactionDate, out DateTime date) && date >= startDate && date <= endDate)
                    .Sum(d => d.DebitAmount);

                int clearedDebts = _database.Table<Debt>().ToList()
                    .Where(d => d.UserId == _currentUser.UserId && d.isCleared && DateTime.TryParse(d.DebtTransactionDate, out DateTime date) && date >= startDate && date <= endDate)
                    .Sum(d => d.DebtAmount);

                return totalInflow + clearedDebts - totalOutflow;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error filtering total transactions amount: {ex.Message}");
                return 0;
            }
        }

        public List<Debt> GetPendingDebtsForDateRange(DateTime startDate, DateTime endDate)
        {
            if (_currentUser == null) return new List<Debt>();
            try
            {
                return _database.Table<Debt>().ToList()
                    .Where(d => d.UserId == _currentUser.UserId && !d.isCleared && DateTime.TryParse(d.DebtTransactionDate, out DateTime date) && date >= startDate && date <= endDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error filtering pending debts: {ex.Message}");
                return new List<Debt>();
            }
        }

        public List<Debt> GetPendingDebtsListForDateRange(DateTime startDate, DateTime endDate)
        {
            if (_currentUser == null) return new List<Debt>();
            try
            {
                return _database.Table<Debt>().ToList()
                    .Where(debt => debt.UserId == _currentUser.UserId && !debt.isCleared &&
                                   DateTime.TryParse(debt.DebtDueDate, out DateTime dueDate) &&
                                   dueDate >= startDate && dueDate <= endDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error filtering pending debts by due date: {ex.Message}");
                return new List<Debt>();
            }
        }


        public List<Transaction> GetAllTransactionsForDateRange(DateTime startDate, DateTime endDate)
        {
            if (_currentUser == null) return new List<Transaction>();
            try
            {
                return _database.Table<Transaction>().Where(t => t.UserId == _currentUser.UserId).ToList()
                    .Where(t =>
                    {
                        if (DateTime.TryParse(t.TransactionDate, out DateTime parsedDate))
                        {
                            return parsedDate >= startDate && parsedDate <= endDate;
                        }
                        return false;
                    })
                    .OrderByDescending(t => DateTime.Parse(t.TransactionDate)) // Ensure valid parsing before sorting
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error filtering transactions: {ex.Message}");
                return new List<Transaction>();
            }
        }
        public string GetUserCurrency()
        {
            if (_currentUser == null) return "NPR"; 

            try
            {
                var user = _database.Table<User>().FirstOrDefault(u => u.UserId == _currentUser.UserId);
                return user?.PreferredCurrency ?? "NPR"; 
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving user currency: {ex.Message}");
                return "NPR";
            }
        }






    }




}
