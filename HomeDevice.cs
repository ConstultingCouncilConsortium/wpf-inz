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
        public DateOnly PurchaseDate { get; set; } // Zmiana z DateTime na DateOnly

        public int WarrantyPeriodMonths { get; set; }

        public byte[] ReceiptImage { get; set; }

        public int UserId { get; set; } // Klucz obcy do powiązania z użytkownikiem

        [Required]
        public DateOnly WarrantyEndDate { get; set; } // Zmiana z DateTime na DateOnl
        [NotMapped]
        public DateTime WarrantyEndDateTime => WarrantyEndDate.ToDateTime(new TimeOnly(0, 0));
    }
}
