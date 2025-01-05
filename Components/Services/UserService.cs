    using System;
    using System.Diagnostics;
    using System.IO;
    using SQLite;
    using BudgetMate.Components.Models;
    using System.Data.Common;

    namespace BudgetMate.Components.Services
    {
        public class UserService
        {
            private SQLiteConnection _database;
            private string _dbPath;

            public UserService()
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop); //desktop path
                string folderPath = Path.Combine(desktopPath, "Test");
             
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                _dbPath = Path.Combine(folderPath, "BudgetMateFinals.db3"); //database path

                _database = new SQLiteConnection(_dbPath); //creating connection

                _database.CreateTable<User>(); //creating user table
                _database.CreateTable<Debt>();
                _database.CreateTable<Debit>();
                _database.CreateTable<Credit>();
                _database.CreateTable<Transaction>();
            _database.CreateTable<Balance>();



            Debug.WriteLine($"Database path: {_dbPath}");
                Console.WriteLine($"Database path: {_dbPath}");
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

            public User LoginUser(string username, string password)
            {
                return _database.Table<User>().FirstOrDefault(u => u.Username == username && u.Password == password); //validating username and password 
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

            public bool AddDebitTransaction(Debit debit)
            {

                if (debit == null)
                {
                    Debug.WriteLine("Debit transaction data is null");
                    return false;
                }

                Debug.WriteLine($"Debit transaction: Title={debit.DebitTransactionTitle}, Amount={debit.DebitAmount}, Tag={debit.DebitTags}");

                if (string.IsNullOrEmpty(debit.DebitTransactionTitle) || debit.DebitAmount <= 0)
                {
                    Debug.WriteLine("Invalid debit transaction data");
                    return false;
                }

                try
                {

                    _database.BeginTransaction();

                    _database.Insert(debit);

                    var transaction = new Transaction
                    {
                        TransactionID = debit.DebitID,
                        TransactionDate = debit.DebitTransactionDate,
                        Amount = debit.DebitAmount,
                        Type = "Debit",
                        Tags = debit.DebitTags,
                        Note = "" // If you need a note field, you can modify accordingly
                    };
                    _database.Insert(transaction); // Insert into Transaction table

                    _database.Commit();

                    Debug.WriteLine("Debit transaction inserted successfully.");
                    return true;
                }
                catch (SQLiteException ex)
                {
                    _database.Rollback();
                    Debug.WriteLine($"Error inserting debit transaction: {ex.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Unexpected error: {ex.Message}");
                    return false;
                }
            }

        public void AutoClearDebts()
        {
            try
            {
                var pendingDebts = GetPendingDebts().OrderBy(debt => debt.DebtDueDate).ToList();
                int totalBalance = GetTotalBalance();

                foreach (var debt in pendingDebts)
                {
                    // Check if the balance is sufficient to clear the debt
                    if (debt.DebtAmount <= totalBalance)
                    {
                        debt.isCleared = true;
                        _database.Update(debt);

                        // Deduct the cleared debt amount from the current balance
                        totalBalance -= debt.DebtAmount;


                        Debug.WriteLine($"Cleared Debt: {debt.DebtAmount}, Remaining Balance: {totalBalance}");
                    }
                    else
                    {
                        break; // Stop processing if balance is insufficient
                    }
                }

                // Recalculate balance after processing debts
                RecalculateBalance();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error auto-clearing debts: {ex.Message}");
            }
        }


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

                // Update balance and clear debts if possible
                RecalculateBalance();
                AutoClearDebts();

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

                    // Now insert into Transaction table
                    var transaction = new Transaction
                    {
                        TransactionID = debt.DebtId,
                        TransactionDate = debt.DebtTransactionDate,
                        Amount = debt.DebtAmount,
                        Type = "Debt",
                        Tags = debt.SourceOfDebt,
                        Note = "" // If you need a note field, you can modify accordingly
                    };
                    _database.Insert(transaction); // Insert into Transaction table

                    _database.Commit();

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

        public bool ClearDebt(int debtId)
        {
            try
            {
                // Get the debt record by its ID
                var debt = _database.Table<Debt>().FirstOrDefault(d => d.DebtId == debtId);

                if (debt != null)
                {
                    // Get the total balance
                    int totalBalance = GetTotalBalance();

                    // Check if debt amount is less than or equal to the total balance
                    if (debt.DebtAmount <= totalBalance)
                    {
                        // Mark the debt as cleared
                        debt.isCleared = true;

                        // Update the debt in the database
                        _database.Update(debt);

                        // Recalculate the total balance
                        RecalculateBalance();

                        Debug.WriteLine($"Debt of amount {debt.DebtAmount} cleared. Total balance updated.");
                        return true;
                    }
                    else
                    {
                        Debug.WriteLine("Not enough balance to clear the debt.");
                        return false; // Not enough balance to clear the debt
                    }
                }

                return false; // Debt not found
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error while clearing debt: {ex.Message}");
                return false; // Return false if there's an error
            }
        }

        public int GetTotalInflows()
        {
            try
            {
                return _database.Table<Credit>().Sum(c => c.CreditAmount);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error calculating total inflows: {ex.Message}");
                return 0;
            }
        }

        public int GetTotalBalance()
        {
            var balance = _database.Table<Balance>().FirstOrDefault();
            return balance?.TotalBalance ?? 0; // Return 0 if balance record doesn't exist
        }


        public void RecalculateBalance()
        {
            try
            {
                // Calculate total inflows, outflows, and remaining debts
                int totalInflows = GetTotalInflows();
                int totalOutflows = GetTotalOutflow();
                int remainingDebt = GetRemainingDebt();

                // Calculate the balance
                int totalBalance = totalInflows - remainingDebt;

                var balance = _database.Table<Balance>().FirstOrDefault();
                if (balance == null)
                {
                    balance = new Balance { TotalBalance = totalBalance };
                    _database.Insert(balance);
                }
                else
                {
                    balance.TotalBalance = totalBalance;
                    _database.Update(balance);
                }


                Debug.WriteLine($"Recalculated Balance: {totalBalance}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error recalculating balance: {ex.Message}");
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
                return _database.Table<Debt>().Where(debt => !debt.isCleared).Sum(d => d.DebtAmount);
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
                // Calculate remaining debt by summing up all debts that are not cleared
                return _database.Table<Debt>().Where(debt => !debt.isCleared).Sum(d => d.DebtAmount);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error calculating remaining debt: {ex.Message}");
                return 0; // Return 0 in case of error
            }
        }

        public int GetClearedDebt()
        {
            try
            {
                // Calculate cleared debt by summing up all debts that are cleared
                return _database.Table<Debt>().Where(debt => debt.isCleared).Sum(d => d.DebtAmount);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error calculating cleared debt: {ex.Message}");
                return 0; // Return 0 in case of error
            }
        }



        public int GetTotalNumberOfTransactions()
        {
            int debitCount = _database.Table<Debit>().Count();

            int creditCount = _database.Table<Credit>().Count();

            int debtCount = _database.Table<Debt>().Count();

            int clearedDebtCount = _database.Table<Debt>().Where(debt => debt.isCleared).Count();

            return debitCount + creditCount + debtCount + clearedDebtCount;
        }


        public int GetTotalTransactionsAmount()
        {
            int totalInflow = _database.Table<Credit>().Sum(credit => credit.CreditAmount);

            int totalOutflow = _database.Table<Debit>().Sum(debit => debit.DebitAmount);

            int totalDebt = _database.Table<Debt>().Sum(debt => debt.DebtAmount);

            return totalInflow + totalDebt - totalOutflow;


        }


    }
}