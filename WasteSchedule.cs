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
        public string WasteType { get; set; } 

        [Required]
        public DateTime Date { get; set; } 

        public int? FrequencyInDays { get; set; } 
        [Required]
        public int UserId { get; set; } 
    }
}
