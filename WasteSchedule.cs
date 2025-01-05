using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace wpf_inz
{
    public class WasteSchedule
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string WasteType { get; set; } // Typ odpadu (np. szkło, papier, plastik, mieszane)

        [Required]
        public DateTime Date { get; set; } // Data wywozu

        public int? FrequencyInDays { get; set; } // Liczba dni między wywozami (null dla jednorazowych)
        [Required]
        public int UserId { get; set; } // Klucz obcy do identyfikacji użytkownika
    }
}
