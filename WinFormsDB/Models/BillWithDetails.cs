using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsDB.Models
{
    public class BillWithDetails
    {
        // Свойства из Bill
        public int BillID { get; set; }
        public int AddressID { get; set; }
        public int TariffID { get; set; }
        public decimal ConsumedVolume { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public bool IsPaid { get; set; }

        // Дополнительные свойства для отображения
        public string TariffName { get; set; }
        public string ServiceName { get; set; }
        public string Street { get; set; }
        public string House { get; set; }
        public string Apartment { get; set; }

        // Вычисляемые свойства для удобства
        public string FullAddress => $"{Street} {House}" + (string.IsNullOrEmpty(Apartment) ? "" : $", кв. {Apartment}");
        public string PaymentStatus => IsPaid ? "Оплачен" : "Не оплачен";
        public string FormattedAmount => Amount.ToString("C2");
        public string FormattedDate => PaymentDate.ToString("dd.MM.yyyy");
    }
}
