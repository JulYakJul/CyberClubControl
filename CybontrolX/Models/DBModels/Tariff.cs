
namespace CybontrolX.DBModels
{
    public class Tariff
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public TimeSpan SessionTime { get; set; }
        public List<DayOfWeek> Days { get; set; } = new List<DayOfWeek>();
        public double Price { get; set; }
        public ICollection<SessionTariff> SessionTariffs { get; set; } = new List<SessionTariff>();
    }
}
