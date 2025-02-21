﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace wpf_inz
{
    public class UnifiedEvent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } 

        [Required]
        public int ReferenceId { get; set; } 

        [Required]
        [MaxLength(50)]
        public string EventType { get; set; } 
    }

}
