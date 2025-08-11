namespace CybontrolX.DBModels
{
    public class Session
    {
        public int Id { get; set; }
        public DateTime SessionStartTime { get; set; }
        public DateTime? SessionEndTime { get; set; }
        public bool IsActive { get; set; }

        public int ClientId { get; set; }
        public int ComputerId { get; set; }
        public int EmployeeId { get; set; }
        public string? PaymentYooKassaId { get; set; }
        public bool PaymentStatus { get; set; }

        public Client Client { get; set; }
        public Computer Computer { get; set; }
        public Employee Employee { get; set; }

        public ICollection<SessionTariff> SessionTariffs { get; set; } = new List<SessionTariff>();
    }
}
