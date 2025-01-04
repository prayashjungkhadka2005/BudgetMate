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

            _dbPath = Path.Combine(folderPath, "BudgetMateDatabase.db3"); //database path

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
            Debug.WriteLine($"Debit transaction: Title={debit.DebitTransactionTitle}, Amount={debit.DebitAmount}");

            if (debit == null || string.IsNullOrEmpty(debit.DebitTransactionTitle) || debit.DebitAmount <= 0)
            {
                Debug.WriteLine("Invalid debit transaction data");
                return false;
            }

            try
            {
                // Begin transaction
                _database.BeginTransaction();

                // Insert debit transaction into the database
                _database.Insert(debit);

                // Commit the transaction
                _database.Commit();

                Debug.WriteLine("Debit transaction inserted successfully.");
                return true;
            }
            catch (SQLiteException ex)
            {
                _database.Rollback(); // Roll back the transaction in case of an error
                Debug.WriteLine($"Error inserting debit transaction: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error: {ex.Message}");
                return false;
            }
        }
    }
}