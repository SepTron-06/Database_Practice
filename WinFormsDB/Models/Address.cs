using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsDB.Models
{
    public class Address
    {
        public int AddressID { get; set; }
        public int ClientID { get; set; }
        public string Street { get; set; }
        public string House { get; set; }
        public string Apartment { get; set; }
        public decimal LivingArea { get; set; }
        public int ResidentsCount { get; set; }

    }
}
