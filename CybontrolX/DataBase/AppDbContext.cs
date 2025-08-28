using Microsoft.EntityFrameworkCore;
using CybontrolX.DBModels;

namespace CybontrolX.DataBase
{
    public class AppDbContext : DbContext
    {
        public DbSet<Client> Clients { get; set; }
        public DbSet<Computer> Computers { get; set; }
        public DbSet<DutySchedule> DutySchedules { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Tariff> Tariffs { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<SessionTariff> SessionTariffs { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            //modelBuilder.Entity<Product>().ToTable("Products");

            modelBuilder.Entity<Employee>()
               .HasOne(e => e.DutySchedule)
               .WithOne(d => d.Employee)
               .HasForeignKey<Employee>(e => e.DutyScheduleId);

            modelBuilder.Entity<Employee>()
                .Property(e => e.Role)
                .HasConversion<string>();
        }
    }
}
