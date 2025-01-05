using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace wpf_inz
{
    public class ConfirmedAppointment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string EventType { get; set; } // Typ wydarzenia (np. techniczny, gazowy, spotkanie, inne)

        [Required]
        public DateTime Date { get; set; } // Data wydarzenia

        public TimeSpan? Time { get; set; } // Opcjonalna godzina wydarzenia

        [MaxLength(255)]
        public string Note { get; set; } // Opis wydarzenia

        [MaxLength(100)]
        public string ContactName { get; set; } // Opcjonalne: imię i nazwisko osoby kontaktowej

        [MaxLength(15)]
        public string ContactPhone { get; set; } // Opcjonalne: numer telefonu osoby kontaktowej

        [Required]
        public int UserId { get; set; } // Klucz obcy identyfikujący użytkownika
    }
}
