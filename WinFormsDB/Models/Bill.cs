using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsDB.Models
{
    public class Bill
    {
        public int BillID { get; set; }
        public int AddressID { get; set; }
        public int TariffID { get; set; } // Заменяем ServiceID на TariffID
        public decimal Amount { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime? PaymentDate { get; set; }
        public bool IsPaid { get; set; }

        // Навигационные свойства
        public Address Address { get; set; }
        public Tariff Tariff { get; set; } // Заменяем Service на Tariff
    }
}