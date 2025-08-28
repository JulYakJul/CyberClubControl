namespace CybontrolX.DBModels
{
    public class Reservation
    {
        public int Id { get; set; }
        public DateTime ReservationStartTime { get; set; }
        public DateTime ReservationEndTime { get; set; }
        public string PaymentType { get; set; }
        public int ClientId { get; set; }
        public int ComputerId { get; set; }
        public int TariffId { get; set; } 

        public Client Client { get; set; }
        public Computer Computer { get; set; }
        public Tariff Tariff { get; set; }
    }
}
