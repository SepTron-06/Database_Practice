using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsDB.Models
{
    public class Client
    {
        public int ClientID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Phone { get; set; }
        public string Email { get; set; }
        //public int AddressId { get; set; }
        //public Address Address { get; set; }
    }
}
