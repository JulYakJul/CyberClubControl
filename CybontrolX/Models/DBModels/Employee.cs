using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CybontrolX.DBModels
{
    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Patronymic { get; set; }
        public string PhoneNumber { get; set; }
        public int? DutyScheduleId { get; set; }
        public string? Status { get; set; }
        public TimeSpan? ShiftStart { get; set; }
        public TimeSpan? ShiftEnd { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string PasswordHash { get; set; }
        public bool EmailConfirmed { get; set; } = false;
        public string? ConfirmationCode { get; set; }
        public DateTime CodeExpiration { get; set; }
        public DateTime? DeletedAt { get; set; }

        [Column(TypeName = "text")]
        public Role Role { get; set; }

        public DutySchedule? DutySchedule { get; set; }
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }

    public enum Role
    {
        [Display(Name = "Управляющий")]
        Manager,

        [Display(Name = "Администратор")]
        Admin
    }
}
