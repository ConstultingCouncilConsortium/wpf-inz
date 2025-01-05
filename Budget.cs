using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace wpf_inz
{
    public class Budget
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; } // Referencja do użytkownika

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } // "Przychody" lub "Koszty"



        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } // Waluta, np. "PLN", "USD"

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; } // Kwota transakcji

        [Required]
        public DateTime Date { get; set; } // Data transakcji
        [MaxLength(250)]
        public string Description { get; set; } // Opcjonalny opis wpisu

    }
}
