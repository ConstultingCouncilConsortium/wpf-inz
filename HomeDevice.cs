using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace wpf_inz
{
    public class HomeDevice
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(100)]
        public string Model { get; set; }

        [Required]
        public DateOnly PurchaseDate { get; set; } 

        public int WarrantyPeriodMonths { get; set; }

        public byte[] ReceiptImage { get; set; }

        public int UserId { get; set; } 

        [Required]
        public DateOnly WarrantyEndDate { get; set; } 
        [NotMapped]
        public DateTime WarrantyEndDateTime => WarrantyEndDate.ToDateTime(new TimeOnly(0, 0));
    }
}
