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
        [AutoIncrement, PrimaryKey]
        public int DebitID { get; set; }
        public string DebitTransactionTitle { get; set; }
        public int DebitAmount { get; set; }
        public string DebitTags {get; set;}

    }
}
