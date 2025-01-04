using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BudgetMate.Components.Models
{
    public class Transaction
    {
        public string Type { get; set; } = "expense";
        public string Title { get; set; }
        public int Amount { get; set; }
        public DateTime? DueDate { get; set; }
        public string SourceOfDebt { get; set; }
        public string SelectedTag { get; set; }
        public string CustomTag { get; set; }
    }
}
