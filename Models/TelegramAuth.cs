using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DormitoryPATDesktop.Models
{
    public class TelegramAuth
    {
        [Key]             
        public long TelegramId { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? Username { get; set; }

        //[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        //public DateTime FirstAuth { get; set; }

        //[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        //public DateTime LastAction { get; set; }

        // Навигационные свойства
        public ICollection<RepairRequests> RepairRequests { get; set; }
        [InverseProperty("TelegramAuth")]
        public ICollection<Complaints> SubmittedComplaints { get; set; }
    }    
}
