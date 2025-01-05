    using SQLite;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    namespace BudgetMate.Components.Models
    {
        public class Balance
        {
            [AutoIncrement, PrimaryKey]
            public int BalanceId { get; set; }
            public int TotalBalance { get; set; }
        }
    }
