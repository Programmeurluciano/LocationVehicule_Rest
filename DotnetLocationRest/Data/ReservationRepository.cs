using Microsoft.Data.SqlClient;
using DotnetLocationRest.DTOs;
using DotnetLocationRest.Models;

namespace DotnetLocationRest.Data
{
    public class ReservationRepository
    {
        private readonly DbConnectionFactory _dbFactory;

        public ReservationRepository(DbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<CalculPrixDto?> CalculerPrixAsync(int vehiculeId, DateTime dateDebut, DateTime dateFin)
        {
            using var connection = _dbFactory.Create();
            await connection.OpenAsync();

            var query = @"
                SELECT v.Id, v.Marque, v.Model, v.Prix, v.Caution, v.Penalite
                FROM Vehicule v
                WHERE v.Id = @VehiculeId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add(new SqlParameter("@VehiculeId", vehiculeId));

            using var reader = await command.ExecuteReaderAsync();
            
            if (!await reader.ReadAsync())
                return null;

            var vehicule = new
            {
                Id = reader.GetInt32(0),
                Marque = reader.GetString(1),
                Model = reader.GetString(2),
                Prix = reader.GetDecimal(3),
                Caution = reader.GetDecimal(4),
                Penalite = reader.GetDecimal(5)
            };

            var nombreJours = (dateFin.Date - dateDebut.Date).Days;
            if (nombreJours <= 0) nombreJours = 1;

            var prixTotal = vehicule.Prix * nombreJours;

            return new CalculPrixDto
            {
                VehiculeId = vehicule.Id,
                VehiculeMarque = vehicule.Marque,
                VehiculeModel = vehicule.Model,
                PrixJour = vehicule.Prix,
                DateDebut = dateDebut,
                DateFin = dateFin,
                NombreJours = nombreJours,
                PrixTotal = prixTotal,
                Caution = vehicule.Caution,
                PenaliteJournaliere = vehicule.Penalite,
                Message = $"Location de {nombreJours} jour(s) à {vehicule.Prix:N0} Ar/jour"
            };
        }

        public async Task<DisponibiliteDto> VerifierDisponibiliteAsync(int vehiculeId, DateTime dateDebut, DateTime dateFin)
        {
            using var connection = _dbFactory.Create();
            await connection.OpenAsync();

            var queryVehicule = "SELECT Disponibilite FROM Vehicule WHERE Id = @VehiculeId";
            using var cmdVehicule = new SqlCommand(queryVehicule, connection);
            cmdVehicule.Parameters.Add(new SqlParameter("@VehiculeId", vehiculeId));
            
            var disponibiliteVehicule = await cmdVehicule.ExecuteScalarAsync();
            
            if (disponibiliteVehicule == null)
            {
                return new DisponibiliteDto
                {
                    VehiculeId = vehiculeId,
                    DateDebut = dateDebut,
                    DateFin = dateFin,
                    EstDisponible = false,
                    Message = "Véhicule introuvable"
                };
            }

            if (!(bool)disponibiliteVehicule)
            {
                return new DisponibiliteDto
                {
                    VehiculeId = vehiculeId,
                    DateDebut = dateDebut,
                    DateFin = dateFin,
                    EstDisponible = false,
                    Message = "Véhicule non disponible à la location"
                };
            }

            var queryReservations = @"
                SELECT DateDebut, DateFin
                FROM Reservation
                WHERE VehiculeId = @VehiculeId
                  AND Status = 1
                  AND (DateDebut <= @DateFin AND DateFin >= @DateDebut)";

            using var cmdReservations = new SqlCommand(queryReservations, connection);
            cmdReservations.Parameters.Add(new SqlParameter("@VehiculeId", vehiculeId));
            cmdReservations.Parameters.Add(new SqlParameter("@DateDebut", dateDebut));
            cmdReservations.Parameters.Add(new SqlParameter("@DateFin", dateFin));

            var periodesReservees = new List<PeriodeReserveeDto>();
            using var reader = await cmdReservations.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                periodesReservees.Add(new PeriodeReserveeDto
                {
                    DateDebut = reader.GetDateTime(0),
                    DateFin = reader.GetDateTime(1)
                });
            }

            var estDisponible = !periodesReservees.Any();

            return new DisponibiliteDto
            {
                VehiculeId = vehiculeId,
                DateDebut = dateDebut,
                DateFin = dateFin,
                EstDisponible = estDisponible,
                Message = estDisponible 
                    ? "Véhicule disponible pour ces dates" 
                    : "Véhicule déjà réservé pour cette période",
                PeriodesReservees = periodesReservees.Any() ? periodesReservees : null
            };
        }

        public async Task<ReservationDetailDto?> CreerReservationAsync(CreateReservationDto dto)
        {
            using var connection = _dbFactory.Create();
            await connection.OpenAsync();

            var disponibilite = await VerifierDisponibiliteAsync(dto.VehiculeId, dto.DateDebut, dto.DateFin);
            
            if (!disponibilite.EstDisponible)
                throw new InvalidOperationException(disponibilite.Message);

            var insertQuery = @"
                INSERT INTO Reservation (ClientId, VehiculeId, DateDebut, DateFin, Status)
                VALUES (@ClientId, @VehiculeId, @DateDebut, @DateFin, 1);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            int reservationId;
            using (var command = new SqlCommand(insertQuery, connection))
            {
                command.Parameters.Add(new SqlParameter("@ClientId", dto.ClientId));
                command.Parameters.Add(new SqlParameter("@VehiculeId", dto.VehiculeId));
                command.Parameters.Add(new SqlParameter("@DateDebut", dto.DateDebut));
                command.Parameters.Add(new SqlParameter("@DateFin", dto.DateFin));

                reservationId = (int)await command.ExecuteScalarAsync();
            }

            return await GetReservationByIdAsync(reservationId);
        }

        public async Task<ReservationDetailDto?> GetReservationByIdAsync(int id)
        {
            using var connection = _dbFactory.Create();
            await connection.OpenAsync();

            var query = @"
                SELECT 
                    r.Id, r.ClientId,
                    c.Nom AS ClientNom, c.Prenom AS ClientPrenom,
                    c.Email AS ClientEmail, c.Contact AS ClientContact,
                    r.VehiculeId,
                    v.Marque AS VehiculeMarque, v.Model AS VehiculeModel,
                    v.Prix AS VehiculePrixJour, v.Caution, v.Penalite AS PenaliteJournaliere,
                    (SELECT TOP 1 ImageUrl FROM VehiculeImage WHERE VehiculeId = v.Id AND EstPrincipale = 1) AS VehiculeImagePrincipale,
                    r.DateDebut, r.DateFin, r.DateRetourVehicule, r.Status
                FROM Reservation r
                INNER JOIN Client c ON r.ClientId = c.Id
                INNER JOIN Vehicule v ON r.VehiculeId = v.Id
                WHERE r.Id = @Id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add(new SqlParameter("@Id", id));

            using var reader = await command.ExecuteReaderAsync();
            
            if (!await reader.ReadAsync())
                return null;

            var dateDebut = reader.GetDateTime(13);
            var dateFin = reader.GetDateTime(14);
            var nombreJours = (dateFin.Date - dateDebut.Date).Days;
            if (nombreJours <= 0) nombreJours = 1;

            var prixJour = reader.GetDecimal(9);
            var prixTotal = prixJour * nombreJours;

            var status = (ReservationStatus)reader.GetInt32(16);
            var statusLibelle = status switch
            {
                ReservationStatus.EnCours => "En cours",
                ReservationStatus.Annulee => "Annulée",
                ReservationStatus.Terminee => "Terminée",
                _ => "Inconnu"
            };

            int? joursRetard = null;
            decimal? montantPenalite = null;
            var dateRetour = reader.IsDBNull(15) ? (DateTime?)null : reader.GetDateTime(15);

            if (dateRetour.HasValue && dateRetour.Value.Date > dateFin.Date)
            {
                joursRetard = (dateRetour.Value.Date - dateFin.Date).Days;
                var penaliteJournaliere = reader.GetDecimal(11);
                montantPenalite = penaliteJournaliere * joursRetard.Value;
            }

            return new ReservationDetailDto
            {
                Id = reader.GetInt32(0),
                ClientId = reader.GetInt32(1),
                ClientNom = reader.GetString(2),
                ClientPrenom = reader.GetString(3),
                ClientEmail = reader.GetString(4),
                ClientContact = reader.GetString(5),
                VehiculeId = reader.GetInt32(6),
                VehiculeMarque = reader.GetString(7),
                VehiculeModel = reader.GetString(8),
                VehiculePrixJour = prixJour,
                VehiculeImagePrincipale = reader.IsDBNull(12) ? null : reader.GetString(12),
                DateDebut = dateDebut,
                DateFin = dateFin,
                DateRetourVehicule = dateRetour,
                NombreJours = nombreJours,
                PrixTotal = prixTotal,
                Caution = reader.GetDecimal(10),
                PenaliteJournaliere = reader.GetDecimal(11),
                Status = status,
                StatusLibelle = statusLibelle,
                JoursRetard = joursRetard,
                MontantPenalite = montantPenalite
            };
        }

        public async Task<List<ReservationListDto>> GetReservationsByClientAsync(int clientId)
        {
            var reservations = new List<ReservationListDto>();

            using var connection = _dbFactory.Create();
            await connection.OpenAsync();

            var query = @"
                SELECT 
                    r.Id,
                    r.VehiculeId,
                    v.Marque AS VehiculeMarque, 
                    v.Model AS VehiculeModel,
                    (SELECT TOP 1 ImageUrl FROM VehiculeImage WHERE VehiculeId = v.Id AND EstPrincipale = 1) AS VehiculeImagePrincipale,
                    r.DateDebut, 
                    r.DateFin,
                    r.DateRetourVehicule,
                    v.Prix AS PrixJour, 
                    v.Caution,
                    v.Penalite AS PenaliteJournaliere,
                    r.Status
                FROM Reservation r
                INNER JOIN Vehicule v ON r.VehiculeId = v.Id
                WHERE r.ClientId = @ClientId
                ORDER BY r.DateDebut DESC";  // ← TRI PAR DATE DÉCROISSANTE

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add(new SqlParameter("@ClientId", clientId));

            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var dateDebut = reader.GetDateTime(5);
                var dateFin = reader.GetDateTime(6);
                var dateRetour = reader.IsDBNull(7) ? (DateTime?)null : reader.GetDateTime(7);
                var nombreJours = (dateFin.Date - dateDebut.Date).Days;
                if (nombreJours <= 0) nombreJours = 1;

                var prixJour = reader.GetDecimal(8);
                var prixTotal = prixJour * nombreJours;
                var penaliteJournaliere = reader.GetDecimal(10);

                var status = (ReservationStatus)reader.GetInt32(11);
                
                int? joursRetard = null;
                decimal? montantPenalite = null;
                
                if (dateRetour.HasValue && dateRetour.Value.Date > dateFin.Date)
                {
                    joursRetard = (dateRetour.Value.Date - dateFin.Date).Days;
                    montantPenalite = penaliteJournaliere * joursRetard.Value;
                }

                var statusLibelle = status switch
                {
                    ReservationStatus.EnCours => "En cours",
                    ReservationStatus.Annulee => "Annulée",
                    ReservationStatus.Terminee when joursRetard.HasValue && joursRetard > 0 => "Terminée avec pénalité",
                    ReservationStatus.Terminee => "Terminée",
                    _ => "Inconnu"
                };

                reservations.Add(new ReservationListDto
                {
                    Id = reader.GetInt32(0),
                    VehiculeId = reader.GetInt32(1),
                    VehiculeMarque = reader.GetString(2),
                    VehiculeModel = reader.GetString(3),
                    VehiculeImagePrincipale = reader.IsDBNull(4) ? null : reader.GetString(4),
                    DateDebut = dateDebut,
                    DateFin = dateFin,
                    DateRetourVehicule = dateRetour,
                    NombreJours = nombreJours,
                    PrixTotal = prixTotal,
                    VehiculePrixJour = prixJour,
                    Caution = reader.GetDecimal(9),
                    PenaliteJournaliere = penaliteJournaliere,
                    JoursRetard = joursRetard,
                    MontantPenalite = montantPenalite,
                    Status = status,
                    StatusLibelle = statusLibelle
                });
            }

            return reservations;
        }
        public async Task<bool> AnnulerReservationAsync(int id)
        {
            using var connection = _dbFactory.Create();
            await connection.OpenAsync();

            var checkQuery = @"
                SELECT DateDebut, Status 
                FROM Reservation 
                WHERE Id = @Id";

            using var checkCmd = new SqlCommand(checkQuery, connection);
            checkCmd.Parameters.Add(new SqlParameter("@Id", id));

            using var reader = await checkCmd.ExecuteReaderAsync();
            
            if (!await reader.ReadAsync())
                return false; 

            var dateDebut = reader.GetDateTime(0);
            var status = (ReservationStatus)reader.GetInt32(1);
            reader.Close();

            if (status != ReservationStatus.EnCours)
                throw new InvalidOperationException("Seules les réservations en cours peuvent être annulées");

            if (dateDebut.Date <= DateTime.Today)
                throw new InvalidOperationException("Impossible d'annuler une réservation déjà commencée");

            var updateQuery = @"
                UPDATE Reservation 
                SET Status = 2
                WHERE Id = @Id";

            using var updateCmd = new SqlCommand(updateQuery, connection);
            updateCmd.Parameters.Add(new SqlParameter("@Id", id));

            var rowsAffected = await updateCmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
    }
}