using System.ComponentModel.DataAnnotations;

namespace DotnetLocationRest.Models
{
    public class Client
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Nom { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Prenom { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string Contact { get; set; } = string.Empty;

        [Required]
        public string MotDePasseHash { get; set; } = string.Empty;
    }
}