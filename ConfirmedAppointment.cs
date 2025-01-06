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
        public string EventType { get; set; } 

        [Required]
        public DateTime Date { get; set; } 

        public TimeSpan? Time { get; set; } 

        [MaxLength(255)]
        public string Note { get; set; } 

        [MaxLength(100)]
        public string ContactName { get; set; } 

        [MaxLength(15)]
        public string ContactPhone { get; set; } 

        [Required]
        public int UserId { get; set; } 
    }
}
