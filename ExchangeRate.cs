using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace wpf_inz
{
    public class ExchangeRate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } // np. "USD", "EUR", "PLN"

        [Required]
        public int Year { get; set; }  // Np. 2024

        [Required]
        public int Month { get; set; } // 1 - 12

        [Required]
        [Column(TypeName = "decimal(18,4)")]
        public decimal RateToPLN { get; set; }
    }
}
