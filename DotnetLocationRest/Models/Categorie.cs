using System.ComponentModel.DataAnnotations;

namespace DotnetLocationRest.Models
{
    public class Categorie
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nom { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}