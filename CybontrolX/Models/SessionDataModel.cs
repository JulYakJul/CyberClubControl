namespace CybontrolX.Models
{
    public class SessionDataModel
    {
        public int ClientId { get; set; }
        public int ComputerId { get; set; }
        public int EmployeeId { get; set; }
        public int[] SelectedTariffs { get; set; }
        public Dictionary<int, int> TariffQuantities { get; set; }
        public DateTime SessionStartTime { get; set; }
        public DateTime SessionEndTime { get; set; }
        public int SessionId { get; set; }
    }
}
