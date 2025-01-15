using SQLite;
using System.Collections.Generic;

namespace BudgetMate.Components.Models
{
    public class Transaction
    {
        [PrimaryKey, AutoIncrement]
        public int TransactionID { get; set; }

        [Indexed]
        public int UserId { get; set; } 
        public string TransactionTitle { get; set; }

        public string TransactionDate { get; set; }
        public int Amount { get; set; }
        public string Type { get; set; } 
        public string Tags { get; set; }
        public string Note { get; set; }

       
    }

}
