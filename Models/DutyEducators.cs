using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DormitoryPATDesktop.Models
{
    public class DutyEducators
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long DutyId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        [ForeignKey("Employees")]
        public long EmployeeId { get; set; }

        [Required]
        [MaxLength(255)]
        public string ContactNumber { get; set; }

        [Required]
        [Column(TypeName = "ENUM('Floor2_4', 'Floor5_7')")]
        public EducatorFloor Floor { get; set; }

        // Навигационное свойство
        public Employees Employees { get; set; }

        [NotMapped]
        public string FloorDisplay => Floor switch
        {
            EducatorFloor.Floor2_4 => "2-4",
            EducatorFloor.Floor5_7 => "5-7",
            _ => Floor.ToString()
        };
    }

    public enum EducatorFloor
    {
        [Display(Name = "Floor2_4")]
        Floor2_4,
        [Display(Name = "Floor5_7")]
        Floor5_7
    }
}
