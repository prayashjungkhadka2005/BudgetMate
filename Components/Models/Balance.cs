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
        [PrimaryKey, AutoIncrement]
        public int BalanceId { get; set; }

        [Indexed]
        public int UserId { get; set; } // FK -> User

        public int TotalBalance { get; set; }
    }

}
