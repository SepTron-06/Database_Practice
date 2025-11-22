using System;

namespace WinFormsDB.Models
{
    public class Address
    {
        public int AddressID { get; set; }
        public int ClientID { get; set; }
        public string Street { get; set; } = string.Empty;
        public string House { get; set; } = string.Empty;
        public string? Apartment { get; set; }
        public decimal LivingArea { get; set; }
        public int ResidentsCount { get; set; }

        // Навигационное свойство
        public Client? Client { get; set; }

        // Вспомогательное свойство для отображения
        public string DisplayAddress => $"{Street}, {House}{(string.IsNullOrEmpty(Apartment) ? "" : $", кв. {Apartment}")}";

        public string DisplayAddressWithClient =>
            $"{Street}, {House}{(string.IsNullOrEmpty(Apartment) ? "" : $", кв. {Apartment}")} - {Client?.LastName} {Client?.FirstName}";
    }
}