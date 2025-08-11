namespace CybontrolX.DBModels
{
    public class Client
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime? DeletedAt { get; set; }

        public ICollection<Session> Sessions { get; set; } = new List<Session>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
