using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace wpf_inz
{
    public class GeneralNote
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Note { get; set; } // Treść notatki

        [Required]
        public DateTime Date { get; set; } // Termin przypomnienia

        public int UserId { get; set; } // Powiązanie z użytkownikiem
    }
}
