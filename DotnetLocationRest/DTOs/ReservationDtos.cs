using DotnetLocationRest.Models;

namespace DotnetLocationRest.DTOs
{
    public class CreateReservationDto
    {
        public int ClientId { get; set; }
        public int VehiculeId { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
    }

    public class ReservationDetailDto
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string ClientNom { get; set; } = string.Empty;
        public string ClientPrenom { get; set; } = string.Empty;
        public string ClientEmail { get; set; } = string.Empty;
        public string ClientContact { get; set; } = string.Empty;
        public int VehiculeId { get; set; }
        public string VehiculeMarque { get; set; } = string.Empty;
        public string VehiculeModel { get; set; } = string.Empty;
        public decimal VehiculePrixJour { get; set; }
        public string? VehiculeImagePrincipale { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
        public DateTime? DateRetourVehicule { get; set; }
        public int NombreJours { get; set; }
        public decimal PrixTotal { get; set; }
        public decimal Caution { get; set; }
        public decimal PenaliteJournaliere { get; set; }
        public ReservationStatus Status { get; set; }
        public string StatusLibelle { get; set; } = string.Empty;
        public int? JoursRetard { get; set; }
        public decimal? MontantPenalite { get; set; }
    }

    public class CalculPrixDto
    {
        public int VehiculeId { get; set; }
        public string VehiculeMarque { get; set; } = string.Empty;
        public string VehiculeModel { get; set; } = string.Empty;
        public decimal PrixJour { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
        public int NombreJours { get; set; }
        public decimal PrixTotal { get; set; }
        public decimal Caution { get; set; }
        public decimal PenaliteJournaliere { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class DisponibiliteDto
    {
        public int VehiculeId { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
        public bool EstDisponible { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<PeriodeReserveeDto>? PeriodesReservees { get; set; }
    }

    public class PeriodeReserveeDto
    {
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
    }

    public class ReservationListDto
    {
        public int Id { get; set; }
        public int VehiculeId { get; set; }
        public string VehiculeMarque { get; set; } = string.Empty;
        public string VehiculeModel { get; set; } = string.Empty;
        public decimal VehiculePrixJour { get; set; }
        public string? VehiculeImagePrincipale { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
        public DateTime? DateRetourVehicule { get; set; }
        public int NombreJours { get; set; }
        public decimal PrixTotal { get; set; }
        public decimal Caution { get; set; }
        public decimal PenaliteJournaliere { get; set; }
        public int? JoursRetard { get; set; }
        public decimal? MontantPenalite { get; set; }
        public ReservationStatus Status { get; set; }
        public string StatusLibelle { get; set; } = string.Empty;
    }
}