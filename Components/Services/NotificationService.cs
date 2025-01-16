using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BudgetMate.Components.Services
{
    public class NotificationService
    {
        public event Action<string, string> OnMessageChanged;

        public void ShowMessage(string message, string type)
        {
            OnMessageChanged?.Invoke(message, type);
        }
    }
}
