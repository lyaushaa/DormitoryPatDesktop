using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DormitoryPATDesktop.Models
{
    public class RepairMaterials
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long MaterialId { get; set; }

        [Required]
        [ForeignKey("RepairRequests")]
        public long RequestId { get; set; }

        [Required]
        [MaxLength(100)]
        public string MaterialName { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal CostPerUnit { get; set; }
    }
}