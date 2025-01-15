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

            _dbPath = Path.Combine(folderPath, "BudgetMate.db3");

            _database = new SQLiteConnection(_dbPath);

            _database.CreateTable<User>();
            _database.CreateTable<Debt>();
            _database.CreateTable<Debit>();
            _database.CreateTable<Credit>();
            _database.CreateTable<Transaction>();
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
            var existingUser = _database.Table<User>().FirstOrDefault(u => u.Username == user.Username); //checking if username exists
            if (existingUser != null)
            {
                return false;
            }

            _database.Insert(user); //inserting user
            return true;
        }





        //        public bool AddDebitTransaction(Debit debit)
        //        {
        //            Debug.WriteLine($"Debit transaction: {debit}");

        //            if (debit == null)
        //            {
        //                Debug.WriteLine("Debit transaction is null");
        //                return false;
        //            }

        //            if (string.IsNullOrEmpty(debit.DebitTransactionTitle) || debit.DebitAmount <= 0)
        //            {
        //                Debug.WriteLine("Invalid debit transaction data: Title or Amount is missing");
        //                return false;
        //            }

        //            try
        //            {
        //                _database.Insert(debit);  // Insert debit transaction into database
        //                Debug.WriteLine("Debit transaction inserted successfully.");
        //                return true;
        //            }
        //            catch (Exception ex)
        //            {
        //                Debug.WriteLine($"Error inserting debit transaction: {ex.Message}");
        //                return false;
        //            }
        //        }




        //    }
        //}

        //public bool AddDebitTransaction(Debit debit)
        //    {

        //        if (debit == null)
        //        {
        //            Debug.WriteLine("Debit transaction data is null");
        //            return false;
        //        }

        //        Debug.WriteLine($"Debit transaction: Title={debit.DebitTransactionTitle}, Amount={debit.DebitAmount}, Tag={debit.DebitTags}");

        //        if (string.IsNullOrEmpty(debit.DebitTransactionTitle) || debit.DebitAmount <= 0)
        //        {
        //            Debug.WriteLine("Invalid debit transaction data");
        //            return false;
        //        }

        //        try
        //        {
        //        int totalBalance = GetTotalBalance();
        //        if(debit.DebitAmount > totalBalance)
        //        {
        //            return false;
        //        }
        //        else
        //        {
        //            _database.BeginTransaction();

        //            _database.Insert(debit);

        //            var transaction = new Transaction
        //            {
        //                TransactionDate = debit.DebitTransactionDate,
        //                Amount = debit.DebitAmount,
        //                Type = "Debit",
        //                Tags = debit.DebitTags,
        //            };
        //            _database.Insert(transaction); // Insert into Transaction table

        //            _database.Commit();
        //            RecalculateBalance();

        //            Debug.WriteLine("Debit transaction inserted successfully.");
        //            return true;
        //        }



        //        }
        //        catch (SQLiteException ex)
        //        {
        //            _database.Rollback();
        //            Debug.WriteLine($"Error inserting debit transaction: {ex.Message}");
        //            return false;
        //        }
        //        catch (Exception ex)
        //        {
        //            Debug.WriteLine($"Unexpected error: {ex.Message}");
        //            return false;
        //        }
        //    }

        public bool AddDebitTransaction(Debit debit)
        {
            if (debit == null || string.IsNullOrEmpty(debit.DebitTransactionTitle) || debit.DebitAmount <= 0)
            {
                Debug.WriteLine("Invalid debit transaction data");
                return false;
            }

            try
            {
                int totalBalance = GetTotalBalance();

                if (debit.DebitAmount > totalBalance)
                {
                    Debug.WriteLine("Transaction denied: Insufficient balance.");
                    return false; 
                }

                _database.BeginTransaction();

                _database.Insert(debit);

                var transaction = new Transaction
                {
                    TransactionDate = debit.DebitTransactionDate,
                    Amount = debit.DebitAmount,
                    Type = "Debit",
                    Tags = debit.DebitTags,
                };
                _database.Insert(transaction);

                _database.Commit();
                RecalculateBalance();  

                Debug.WriteLine("Debit transaction inserted successfully.");
                return true;
            }
            catch (SQLiteException ex)
            {
                _database.Rollback();
                Debug.WriteLine($"Error inserting debit transaction: {ex.Message}");
                return false;
            }
        }


        //public void AutoClearDebts()
        //{
        //    try
        //    {
        //        var pendingDebts = GetPendingDebts().OrderBy(debt => debt.DebtDueDate).ToList();
        //        int totalBalance = GetTotalBalance();

        //        foreach (var debt in pendingDebts)
        //        {
        //            // Check if the balance is sufficient to clear the debt
        //            if (debt.DebtAmount <= totalBalance)
        //            {
        //                debt.isCleared = true;
        //                _database.Update(debt);

        //                // Deduct the cleared debt amount from the current balance
        //                totalBalance -= debt.DebtAmount;


        //                Debug.WriteLine($"Cleared Debt: {debt.DebtAmount}, Remaining Balance: {totalBalance}");
        //            }
        //            else
        //            {
        //                break; // Stop processing if balance is insufficient
        //            }
        //        }

        //        // Recalculate balance after processing debts
        //        RecalculateBalance();
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"Error auto-clearing debts: {ex.Message}");
        //    }
        //}


        public bool AddCreditTransaction(Credit credit)
        {
            if (credit == null || string.IsNullOrEmpty(credit.CreditTransactionTitle) || credit.CreditAmount <= 0)
            {
                Debug.WriteLine("Invalid credit transaction data");
                return false;
            }

            try
            {
                _database.BeginTransaction();

                _database.Insert(credit);

                var transaction = new Transaction
                {
                    TransactionID = credit.CreditID,
                    TransactionDate = credit.CreditTransactionDate,
                    Amount = credit.CreditAmount,
                    Type = "Credit",
                    Tags = credit.CreditTags
                };
                _database.Insert(transaction);

                _database.Commit();

                // Update balance if possible
                RecalculateBalance();
                

                Debug.WriteLine("Credit transaction inserted successfully");
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
                if (debt == null)
                {
                    Debug.WriteLine("Debt transaction data is null");
                    return false;
                }

                if (string.IsNullOrEmpty(debt.DebtTransactionTitle) || debt.DebtAmount <= 0 || debt.DebtDueDate == default)
                {
                    Debug.WriteLine("Invalid debt transaction data");
                    return false;
                }

                Debug.WriteLine($"Debt transaction: Title={debt.DebtTransactionTitle}, Amount={debt.DebtAmount}, DueDate={debt.DebtDueDate}, SourceOfDebt={debt.SourceOfDebt}");

                try
                {
                    _database.BeginTransaction();

                    _database.Insert(debt);

                    



                    _database.Commit();
                RecalculateBalance();

                Debug.WriteLine("Debt transaction inserted successfully.");
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

          
        public List<Debt> GetPendingDebts()
        {
            return _database.Table<Debt>().Where(debt => !debt.isCleared).ToList();
        }




        public List<Transaction> GetAllTransactions()
            {
                return _database.Table<Transaction>().OrderBy(t => t.TransactionDate).ToList();
            }

            public bool UpdateNote(int transactionId, string note)
            {
                try
                {
                    var transaction = _database.Table<Transaction>().FirstOrDefault(t => t.TransactionID == transactionId);
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

        //public bool ClearDebt(int debtId)
        //{
        //    try
        //    {
        //        var debt = _database.Table<Debt>().FirstOrDefault(d => d.DebtId == debtId);

        //        if (debt != null)
        //        {
        //            int totalInflow = GetTotalInflows();

        //            if (debt.DebtAmount <= totalInflow)
        //            {
        //                _database.BeginTransaction(); 
        //                debt.isCleared = true;

        //                _database.Update(debt);

        //                var transaction = new Transaction
        //                {
        //                    TransactionDate = DateTime.Now.ToString("yyyy-MM-dd"),
        //                    Amount = debt.DebtAmount,
        //                    Type = "Debt Cleared", 
        //                    Tags = debt.SourceOfDebt,
        //                    Note = "Debt paid successfully"
        //                };
        //                _database.Insert(transaction);

        //                _database.Commit(); 

        //                RecalculateBalance();

        //                Debug.WriteLine($"Debt of ${debt.DebtAmount} cleared successfully.");
        //                return true;
        //            }
        //            else
        //            {
        //                Debug.WriteLine("Not enough balance to clear the debt.");
        //                return false; 
        //            }
        //        }

        //        return false; 
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"Error while clearing debt: {ex.Message}");
        //        return false; 
        //    }
        //}


        public bool ClearDebt(int debtId)
        {
            try
            {
                var debt = _database.Table<Debt>().FirstOrDefault(d => d.DebtId == debtId);
                if (debt != null)
                {
                    int availableBalance = GetTotalBalance();
                    if (debt.DebtAmount > availableBalance)
                    {
                        Debug.WriteLine("Not enough balance to clear the debt.");
                        return false;
                    }

                    _database.BeginTransaction();
                    debt.isCleared = true;
                    _database.Update(debt);

                    var transaction = new Transaction
                    {
                        TransactionDate = DateTime.Now.ToString("yyyy-MM-dd"),
                        Amount = debt.DebtAmount,
                        Type = "Debt Cleared",
                        Tags = debt.SourceOfDebt,
                        Note = "Debt paid successfully"
                    };
                    _database.Insert(transaction);
                    _database.Commit();

                    RecalculateBalance();
                    Debug.WriteLine($"Debt of ${debt.DebtAmount} cleared successfully.");
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


        public int GetTotalInflows()
        {
            try
            {
                int totalInflows = _database.Table<Credit>().Sum(c => c.CreditAmount);
                int totalClearedDebts = _database.Table<Debt>().Where(d => d.isCleared).Sum(d => d.DebtAmount);
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
                return _database.Table<Debit>().Sum(d => d.DebitAmount);
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
                return _database.Table<Debt>().Sum(d => d.DebtAmount);
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
                return _database.Table<Debt>().Where(debt => !debt.isCleared).Sum(d => d.DebtAmount);
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
               
                return _database.Table<Debt>().Where(debt => debt.isCleared).Sum(d => d.DebtAmount);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error calculating cleared debt: {ex.Message}");
                return 0; 
            }
        }



        public int GetTotalNumberOfTransactions()
        {
            int debitCount = _database.Table<Debit>().Count();

            int creditCount = _database.Table<Credit>().Count();


            int clearedDebtCount = _database.Table<Debt>().Where(debt => debt.isCleared).Count();

            return debitCount + creditCount  + clearedDebtCount;
        }


        public int GetTotalTransactionsAmount()
        {
            int totalInflow = _database.Table<Credit>().Sum(c => c.CreditAmount);
            int totalOutflow = _database.Table<Debit>().Sum(d => d.DebitAmount);
            int clearedDebts = _database.Table<Debt>().Sum(d => d.DebtAmount); 

            return totalInflow + clearedDebts - totalOutflow;
        }


        public int GetTotalBalance()
        {
            var balance = _database.Table<Balance>().FirstOrDefault();
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
                int totalInflows = GetTotalInflows();
                int totalOutflows = GetTotalOutflow();
                int debts = GetTotalDebt();
                int totalBalance = totalInflows - totalOutflows + debts;

                _database.BeginTransaction();
                _database.Execute("DELETE FROM Balance");
                _database.Insert(new Balance { TotalBalance = totalBalance });
                _database.Commit();

                Debug.WriteLine($"Updated Balance: {totalBalance}");
            }
            catch (Exception ex)
            {
                _database.Rollback();
                Debug.WriteLine($"Error recalculating balance: {ex.Message}");
            }
        }


        public List<Debt> GetOverdueDebts()
        {
            var allDebts = _database.Table<Debt>().Where(debt => !debt.isCleared).ToList();

            return allDebts
                .Where(debt => DateTime.TryParse(debt.DebtDueDate, out DateTime dueDate) && dueDate < DateTime.Now)
                .ToList();
        }

      

        public List<Debt> GetClearedDebts()
        {
            try
            {
                return _database.Table<Debt>().Where(debt => debt.isCleared).ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving cleared debts: {ex.Message}");
                return new List<Debt>(); // Return an empty list if there's an error
            }
        }

      


        public int GetTotalInflowsForDateRange(DateTime startDate, DateTime endDate)
        {
            try
            {
                var inflows = _database.Table<Credit>().ToList()
                    .Where(c => DateTime.TryParse(c.CreditTransactionDate, out DateTime date) && date >= startDate && date <= endDate)
                    .Sum(c => c.CreditAmount);

                var clearedDebts = _database.Table<Debt>().ToList()
                    .Where(d => d.isCleared && DateTime.TryParse(d.DebtTransactionDate, out DateTime date) && date >= startDate && date <= endDate)
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
            try
            {
                return _database.Table<Debit>().ToList()
                    .Where(d => DateTime.TryParse(d.DebitTransactionDate, out DateTime date) && date >= startDate && date <= endDate)
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
            try
            {
                return _database.Table<Debt>().ToList()
                    .Where(d => DateTime.TryParse(d.DebtTransactionDate, out DateTime date) && date >= startDate && date <= endDate)
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
            try
            {
                return _database.Table<Debt>().ToList()
                    .Where(d => !d.isCleared && DateTime.TryParse(d.DebtTransactionDate, out DateTime date) && date >= startDate && date <= endDate)
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
            try
            {
                return _database.Table<Debt>().ToList()
                    .Where(d => d.isCleared && DateTime.TryParse(d.DebtTransactionDate, out DateTime date) && date >= startDate && date <= endDate)
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
            try
            {
                // Fetch all cleared debts
                var allClearedDebts = _database.Table<Debt>().Where(debt => debt.isCleared).ToList();

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
            try
            {
                int debitCount = _database.Table<Debit>().ToList()
                    .Count(d => DateTime.TryParse(d.DebitTransactionDate, out DateTime date) && date >= startDate && date <= endDate);

                int creditCount = _database.Table<Credit>().ToList()
                    .Count(c => DateTime.TryParse(c.CreditTransactionDate, out DateTime date) && date >= startDate && date <= endDate);

                int clearedDebtCount = _database.Table<Debt>().ToList()
                    .Count(d => d.isCleared && DateTime.TryParse(d.DebtTransactionDate, out DateTime date) && date >= startDate && date <= endDate);

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
            try
            {
                int totalInflow = _database.Table<Credit>().ToList()
                    .Where(c => DateTime.TryParse(c.CreditTransactionDate, out DateTime date) && date >= startDate && date <= endDate)
                    .Sum(c => c.CreditAmount);

                int totalOutflow = _database.Table<Debit>().ToList()
                    .Where(d => DateTime.TryParse(d.DebitTransactionDate, out DateTime date) && date >= startDate && date <= endDate)
                    .Sum(d => d.DebitAmount);

                int clearedDebts = _database.Table<Debt>().ToList()
                    .Where(d => d.isCleared && DateTime.TryParse(d.DebtTransactionDate, out DateTime date) && date >= startDate && date <= endDate)
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
            try
            {
                return _database.Table<Debt>().ToList()
                    .Where(d => !d.isCleared && DateTime.TryParse(d.DebtTransactionDate, out DateTime date) && date >= startDate && date <= endDate)
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
            try
            {
                return _database.Table<Debt>().ToList()
                    .Where(debt => !debt.isCleared &&
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
            try
            {
                return _database.Table<Transaction>().ToList()
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






    }




}
