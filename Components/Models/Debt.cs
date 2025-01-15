using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BudgetMate.Components.Models
{
    public class Debt
    {
        [PrimaryKey, AutoIncrement]
        public int DebtId { get; set; }

        [Indexed]
        public int UserId { get; set; }
        public int TransactionID { get; set; } 
        public string DebtTransactionTitle { get; set; }
        public string DebtDueDate { get; set; }
        public int DebtAmount { get; set; }
        public string SourceOfDebt { get; set; }
        public string DebtTransactionDate { get; set; }
        public bool isCleared { get; set; }
    }

}
