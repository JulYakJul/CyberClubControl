
using System.ComponentModel.DataAnnotations;

namespace CybontrolX.DBModels
{
    public class SessionTariff
    {
        [Key]
        public int Id { get; set; }

        public int SessionId { get; set; }
        public Session Session { get; set; }

        public int TariffId { get; set; }
        public Tariff Tariff { get; set; }

        public int Quantity { get; set; } // Количество одинаковых тарифов
    }
}
