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
        public string Currency { get; set; } 

        [Required]
        public int Year { get; set; }  

        [Required]
        public int Month { get; set; } 

        [Required]
        [Column(TypeName = "decimal(18,4)")]
        public decimal RateToPLN { get; set; }
    }
}
