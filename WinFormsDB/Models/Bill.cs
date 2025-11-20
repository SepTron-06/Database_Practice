
using System;

namespace WinFormsDB.Models
{
    public class Bill
    {
        public int BillID { get; set; }
        public int AddressID { get; set; }
        public int ServiceID { get; set; }
        public decimal Amount { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime? PaymentDate { get; set; }
        public bool IsPaid { get; set; }

        // Навигационные свойства
        public Address? Address { get; set; }
        public Service? Service { get; set; }
    }
}