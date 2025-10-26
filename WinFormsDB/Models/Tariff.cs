using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsDB.Models
{
    public  class Tariff
    {
        //public int TariffID { get; set; }
        //public int ServiceID { get; set; }
        //public string TariffName { get; set; }
        //public decimal Rate { get; set; }
        //public string Unit { get; set; }
        public int TariffID { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public decimal PricePerSquareMeter { get; set; }
        public decimal PricePerPerson { get; set; }
        public decimal PricePerUnit { get; set; }


    }
}
