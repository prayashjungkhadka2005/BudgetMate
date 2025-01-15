using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BudgetMate.Components.Models
{
    public class Debit
    {
        [PrimaryKey, AutoIncrement]
        public int DebitID { get; set; }

        [Indexed]
        public int UserId { get; set; }
        public int TransactionID { get; set; }  // Foreign Key to Transaction

        public string DebitTransactionTitle { get; set; }
        public int DebitAmount { get; set; }
        public string DebitTags { get; set; }
        public string DebitTransactionDate { get; set; }
    }
}
