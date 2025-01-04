using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BudgetMate.Components.Models
{
    public class Credit
    {
        [AutoIncrement, PrimaryKey]
        public int CreditID { get; set; }
        public string CreditTransactionTitle { get; set; }
        public int CreditAmount { get; set; }
        public string CreditTags { get; set; }
    }
}
