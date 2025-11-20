namespace WinFormsDB.Models
{
    public class Tariff
    {
        public int TariffID { get; set; }
        public int ServiceID { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public decimal PricePerSquareMeter { get; set; }
        public decimal PricePerPerson { get; set; }
        public decimal PricePerUnit { get; set; }
    }
}