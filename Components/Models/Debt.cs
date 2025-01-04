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
        [AutoIncrement, PrimaryKey]
        public int DebID { get; set; }
        public string DebtTransactionTitle { get; set; }
        public DateTime DebtDueDate { get; set; }
        public int DebtAmount { get; set; }
        public string SourceOfDebt { get; set; }

    }
}
