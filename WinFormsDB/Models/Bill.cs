//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace WinFormsDB.Models
//{

//    public class Bill
//    {
//        public int BillID { get; set; }
//        public int ClientID { get; set; }
//        public int ServiceID { get; set; }
//        public decimal Amount { get; set; }
//        public DateTime PaymentDate { get; set; }
//        public bool IsPaid { get; set; }
//    }

//    public class BillWithDetails
//    {
//        public int BillID { get; set; }
//        public string ClientName { get; set; } = string.Empty;
//        public string ServiceName { get; set; } = string.Empty;
//        public decimal Amount { get; set; }
//        public DateTime PaymentDate { get; set; }
//        public bool IsPaid { get; set; }
//    }

//}
namespace WinFormsDB.Models
{
    public class Bill
    {
        public int BillID { get; set; }
        public int AddressID { get; set; }
        public int TariffID { get; set; }
        public decimal ConsumedVolume { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public bool IsPaid { get; set; }
    }

    public class BillWithDetails
    {
        public int BillID { get; set; }
        public int AddressID { get; set; }
        public int TariffID { get; set; }
        public decimal ConsumedVolume { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public bool IsPaid { get; set; }
        public string TariffName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string House { get; set; } = string.Empty;
        public string? Apartment { get; set; }
        public string ClientName { get; set; } = string.Empty;
    }
}