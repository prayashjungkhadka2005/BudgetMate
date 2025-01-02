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
    }
}
