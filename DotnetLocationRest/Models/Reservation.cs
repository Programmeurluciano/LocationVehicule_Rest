using System.ComponentModel.DataAnnotations;

namespace DotnetLocationRest.Models
{
    public class Reservation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClientId { get; set; }

        [Required]
        public int VehiculeId { get; set; }

        [Required]
        public DateTime DateDebut { get; set; }

        [Required]
        public DateTime DateFin { get; set; }

        public DateTime? DateRetourVehicule { get; set; }

        [Required]
        public ReservationStatus Status { get; set; } = ReservationStatus.EnCours;
    }

    public enum ReservationStatus
    {
        EnCours = 1,
        Annulee = 2,
        Terminee = 3
    }
}