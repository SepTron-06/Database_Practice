

using System.Collections.Generic;

namespace WinFormsDB.Models
{
    public class Client
    {
        public int ClientID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Phone { get; set; }
        public string Email { get; set; }
        public string FullName => $"{LastName} {FirstName}";

        // Навигационные свойства
        public List<Address> Addresses { get; set; } = new List<Address>();
        public List<Bill> Bills { get; set; } = new List<Bill>();
    }
}
