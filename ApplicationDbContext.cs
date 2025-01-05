using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpf_inz
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<HomeDevice> HomeDevices { get; set; }
        public DbSet<WasteSchedule> WasteSchedules { get; set; }
        public DbSet<GeneralNote> GeneralNotes { get; set; }
        public DbSet<ConfirmedAppointment> ConfirmedAppointments { get; set; }
        public DbSet<UnifiedEvent> UnifiedEvents { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Budget> Budgets { get; set; }
        public DbSet<ExchangeRate> ExchangeRates { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql("Server=localhost;Database=appdb;User=inzUser;Password=admin123!@#;",
                new MySqlServerVersion(new Version(8, 0, 30)));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasKey(u => u.Id);

            modelBuilder.Entity<User>()
                .Property(u => u.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<HomeDevice>()
                .Property(hd => hd.PurchaseDate)
                .HasConversion<DateOnlyConverter, DateOnlyComparer>(); // Obsługa DateOnly

            modelBuilder.Entity<HomeDevice>()
                .Property(hd => hd.WarrantyEndDate)
                .HasConversion<DateOnlyConverter, DateOnlyComparer>(); // Obsługa DateOnly
            modelBuilder.Entity<HomeDevice>()
           .Property(hd => hd.ReceiptImage)
           .HasColumnType("LONGBLOB"); // Upewnij się, że typ kolumny jest odpowiedni do przechowywania dużych obrazów
            modelBuilder.Entity<Budget>()
        .HasKey(b => b.Id);

            modelBuilder.Entity<Budget>()
                .Property(b => b.Currency)
                .HasMaxLength(10)
                .IsRequired();

            modelBuilder.Entity<Budget>()
                .Property(b => b.Category)
                .HasMaxLength(50)
                .IsRequired();


            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ExchangeRate>().HasData(
            new ExchangeRate
            {
                Id = 1,
                Currency = "PLN",
                Year = 2024,
                Month = 1,
                RateToPLN = 1.00m
            },
            new ExchangeRate
            {
                Id = 2,
                Currency = "USD",
                Year = 2024,
                Month = 1,
                RateToPLN = 4.20m
            },
            new ExchangeRate
            {
                Id = 3,
                Currency = "EUR",
                Year = 2024,
                Month = 1,
                RateToPLN = 4.50m
            }
        );
        }
        public class DateOnlyConverter : ValueConverter<DateOnly, DateTime>
        {
            public DateOnlyConverter() : base(
                d => d.ToDateTime(TimeOnly.MinValue),
                d => DateOnly.FromDateTime(d))
            { }
        }

        public class DateOnlyComparer : ValueComparer<DateOnly>
        {
            public DateOnlyComparer() : base(
                (d1, d2) => d1 == d2,
                d => d.GetHashCode(),
                d => d)
            { }
        }
    }
}
