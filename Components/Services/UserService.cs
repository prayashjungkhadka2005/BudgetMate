using System;
using System.Diagnostics;
using System.IO;
using SQLite;
using BudgetMate.Components.Models;

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

            _dbPath = Path.Combine(folderPath, "BudgetMate.db3"); //database path

            _database = new SQLiteConnection(_dbPath); //creating connection

            _database.CreateTable<User>(); //creating user table
            _database.CreateTable<Debt>();
            _database.CreateTable<Debit>();
            _database.CreateTable<Credit>();


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

        public bool AddCreditTransaction(Credit credit)
        {

            if (credit == null)
            {
                Debug.WriteLine("Credit transaction data is null");
                return false;
            }

            Debug.WriteLine($"Credit transaction: Title={credit.CreditTransactionTitle}, Amount={credit.CreditAmount}, Tag={credit.CreditTags}");

            if ( string.IsNullOrEmpty(credit.CreditTransactionTitle) || credit.CreditAmount <= 0)
            {
                Debug.WriteLine("Invalid credit transaction data");
                return false;
            }

            try
            {
                
                _database.BeginTransaction();

                _database.Insert(credit);

                _database.Commit();

                Debug.WriteLine("Credit transaction inserted successfully");
                return true;
            }
            catch (SQLiteException ex)
            {
                _database.Rollback(); 
                Debug.WriteLine($"Error inserting credit transaction: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error: {ex.Message}");
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

        public int GetTotalNumberOfTransactions()
        {
            int debitCount = _database.Table<Debit>().Count();

            int creditCount = _database.Table<Credit>().Count();

            int debtCount = _database.Table<Debt>().Count();

            //int clearedDebtCount = _database.Table<Debt>().Where(debt => debt.IsCleared).Count();

            return debitCount + creditCount + debtCount;
        }

       
            public int GetTotalTransactionsAmount()
        {
            int totalInflow = _database.Table<Credit>().Sum(credit => credit.CreditAmount);

            int totalOutflow = _database.Table<Debit>().Sum(debit => debit.DebitAmount);

            int totalDebt = _database.Table<Debt>().Sum(debt => debt.DebtAmount);

            return totalInflow + totalDebt - totalOutflow;
        

    }

        public List<Debt> GetPendingDebts()
        {
            return _database.Table<Debt>().ToList();
        }



    }
}