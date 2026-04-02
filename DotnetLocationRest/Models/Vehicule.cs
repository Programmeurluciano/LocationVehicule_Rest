using System.ComponentModel.DataAnnotations;

namespace DotnetLocationRest.Models
{
    public class Vehicule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CategorieId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Marque { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Model { get; set; } = string.Empty;

        [Range(1900, 2100)]
        public int Annee { get; set; }

        [Required]
        [MaxLength(50)]
        public string Transmission { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Carburant { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal Prix { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Caution { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Penalite { get; set; }

        public bool Disponibilite { get; set; }
    }
}