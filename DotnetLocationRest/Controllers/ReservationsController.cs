using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DotnetLocationRest.Data;
using DotnetLocationRest.DTOs;
using DotnetLocationRest.Services;
using DotnetLocationRest.Models;

namespace DotnetLocationRest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservationsController : ControllerBase
    {
        private readonly ReservationRepository _repository;
        private readonly JwtService _jwtService;
        private readonly ILogger<ReservationsController> _logger;

        public ReservationsController(
            ReservationRepository repository,
            JwtService jwtService,
            ILogger<ReservationsController> logger)
        {
            _repository = repository;
            _jwtService = jwtService;
            _logger = logger;
        }

        [HttpPost("calculer-prix")]
        [ProducesResponseType(typeof(CalculPrixDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<CalculPrixDto>> CalculerPrix([FromBody] CalculerPrixRequest request)
        {
            try
            {
                var calcul = await _repository.CalculerPrixAsync(
                    request.VehiculeId,
                    request.DateDebut,
                    request.DateFin);

                if (calcul == null)
                    return NotFound(new { message = "Véhicule introuvable" });

                return Ok(calcul);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du calcul de prix");
                return StatusCode(500, new { message = "Erreur interne du serveur", details = ex.Message });
            }
        }

        [HttpPost("verifier-disponibilite")]
        [ProducesResponseType(typeof(DisponibiliteDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<DisponibiliteDto>> VerifierDisponibilite([FromBody] VerifierDisponibiliteRequest request)
        {
            try
            {
                var disponibilite = await _repository.VerifierDisponibiliteAsync(
                    request.VehiculeId,
                    request.DateDebut,
                    request.DateFin);

                return Ok(disponibilite);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification de disponibilité");
                return StatusCode(500, new { message = "Erreur interne du serveur", details = ex.Message });
            }
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(ReservationDetailDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ReservationDetailDto>> CreerReservation([FromBody] CreateReservationRequestDto request)
        {
            try
            {
                var clientId = _jwtService.GetClientIdFromToken(User);

                if (clientId == null)
                    return Unauthorized(new { message = "Token invalide" });

                if (request.DateDebut.Date < DateTime.Today)
                    return BadRequest(new { message = "La date de début ne peut pas être dans le passé" });

                if (request.DateFin <= request.DateDebut)
                    return BadRequest(new { message = "La date de fin doit être après la date de début" });

                var dto = new CreateReservationDto
                {
                    ClientId = clientId.Value,
                    VehiculeId = request.VehiculeId,
                    DateDebut = request.DateDebut,
                    DateFin = request.DateFin
                };

                var reservation = await _repository.CreerReservationAsync(dto);

                if (reservation == null)
                    return StatusCode(500, new { message = "Erreur lors de la création de la réservation" });

                _logger.LogInformation(
                    "Réservation créée: ID {Id}, Client {ClientId}, Véhicule {VehiculeId}",
                    reservation.Id,
                    reservation.ClientId,
                    reservation.VehiculeId);

                return CreatedAtAction(nameof(GetReservation), new { id = reservation.Id }, reservation);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la réservation");
                return StatusCode(500, new { message = "Erreur interne du serveur", details = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(ReservationDetailDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<ReservationDetailDto>> GetReservation(int id)
        {
            try
            {
                var clientId = _jwtService.GetClientIdFromToken(User);
                if (clientId == null)
                    return Unauthorized(new { message = "Token invalide" });

                var reservation = await _repository.GetReservationByIdAsync(id);

                if (reservation == null)
                    return NotFound(new { message = $"Réservation avec l'ID {id} introuvable" });

                if (reservation.ClientId != clientId.Value)
                    return Forbid();

                return Ok(reservation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la réservation {Id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur", details = ex.Message });
            }
        }

        [HttpGet("mes-reservations")]
        [Authorize]
        [ProducesResponseType(typeof(List<ReservationListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ReservationListDto>>> GetMesReservations()
        {
            try
            {
                var clientId = _jwtService.GetClientIdFromToken(User);
                if (clientId == null)
                    return Unauthorized(new { message = "Token invalide" });

                var reservations = await _repository.GetReservationsByClientAsync(clientId.Value);

                _logger.LogInformation("Récupération de {Count} réservations pour le client {ClientId}",
                    reservations.Count, clientId.Value);

                return Ok(reservations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des réservations");
                return StatusCode(500, new { message = "Erreur interne du serveur", details = ex.Message });
            }
        }

        [HttpPut("{id}/annuler")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> AnnulerReservation(int id)
        {
            try
            {
                var clientId = _jwtService.GetClientIdFromToken(User);
                if (clientId == null)
                    return Unauthorized(new { message = "Token invalide" });

                var reservation = await _repository.GetReservationByIdAsync(id);
                if (reservation == null)
                    return NotFound(new { message = "Réservation introuvable" });

                if (reservation.ClientId != clientId.Value)
                    return Forbid();

                var success = await _repository.AnnulerReservationAsync(id);

                if (!success)
                    return BadRequest(new { message = "Impossible d'annuler cette réservation" });

                _logger.LogInformation("Réservation {Id} annulée par le client {ClientId}", id, clientId.Value);

                return Ok(new { message = "Réservation annulée avec succès" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'annulation de la réservation {Id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur", details = ex.Message });
            }
        }

        [HttpGet("{id}/facture")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> TelechargerFacture(int id)
        {
            try
            {
                var clientId = _jwtService.GetClientIdFromToken(User);
                if (clientId == null)
                    return Unauthorized(new { message = "Token invalide" });

                var reservation = await _repository.GetReservationByIdAsync(id);

                if (reservation == null)
                    return NotFound(new { message = "Réservation introuvable" });

                if (reservation.ClientId != clientId.Value)
                    return Forbid();

                var pdfService = new PdfService();
                var pdfBytes = pdfService.GenererFacture(reservation);

                return File(pdfBytes, "application/pdf", $"Facture_Reservation_{id}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur génération PDF pour réservation {Id}", id);
                return StatusCode(500, new { message = "Erreur lors de la génération du PDF" });
            }
        }

        [HttpGet("{id}/recu")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> TelechargerRecu(int id)
        {
            try
            {
                var clientId = _jwtService.GetClientIdFromToken(User);
                if (clientId == null)
                    return Unauthorized(new { message = "Token invalide" });

                var reservation = await _repository.GetReservationByIdAsync(id);

                if (reservation == null)
                    return NotFound(new { message = "Réservation introuvable" });

                if (reservation.ClientId != clientId.Value)
                    return Forbid();

                // Vérifier que la réservation est terminée
                if (reservation.Status != Models.ReservationStatus.Terminee)
                    return BadRequest(new { message = "Le reçu n'est disponible que pour les réservations terminées" });

                var recuService = new RecuPdfService();
                var pdfBytes = recuService.GenererRecu(reservation);

                return File(pdfBytes, "application/pdf", $"Recu_Reservation_{id}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur génération reçu PDF pour réservation {Id}", id);
                return StatusCode(500, new { message = "Erreur lors de la génération du reçu" });
            }
        }
    }

    public class CreateReservationRequestDto
    {
        public int VehiculeId { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
    }

    public class CalculerPrixRequest
    {
        public int VehiculeId { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
    }

    public class VerifierDisponibiliteRequest
    {
        public int VehiculeId { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
    }
}