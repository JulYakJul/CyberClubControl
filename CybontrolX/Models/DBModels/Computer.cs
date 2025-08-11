using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CybontrolX.DBModels
{
    public class Computer
    {
        public int Id { get; set; }
        public string ComputerIP { get; set; }
        public bool Status { get; set; } // Активная / неактивная сессия
        public int? CurrentClientId { get; set; }
        public DateTime? DeletedAt { get; set; }

        public Client? CurrentClient { get; set; }
        public ICollection<Session> Session { get; set; } = new List<Session>();
    }
}
