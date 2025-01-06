using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpf_inz
{
    public class Notification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int UnifiedEventId { get; set; } 

        [Required]
        public int UserId { get; set; } 

        [Required]
        public string Message { get; set; }

        [Required]
        public bool IsRead { get; set; } = false; 

        public DateTime CreatedAt { get; set; } = DateTime.Now; 
    }

}
