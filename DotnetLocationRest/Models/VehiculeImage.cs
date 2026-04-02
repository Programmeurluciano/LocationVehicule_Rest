using System.ComponentModel.DataAnnotations;

namespace DotnetLocationRest.Models
{
    public class VehiculeImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ImageUrl { get; set; } = string.Empty;

        public bool EstPrincipale { get; set; }

        public int VehiculeId { get; set; }
    }
}