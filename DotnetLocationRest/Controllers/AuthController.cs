using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DotnetLocationRest.Data;
using DotnetLocationRest.DTOs;
using DotnetLocationRest.Services;

namespace DotnetLocationRest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthRepository _authRepository;
        private readonly JwtService _jwtService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            AuthRepository authRepository,
            JwtService jwtService,
            ILogger<AuthController> logger)
        {
            _authRepository = authRepository;
            _jwtService = jwtService;
            _logger = logger;
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Email) || !dto.Email.Contains("@"))
                    return BadRequest(new { message = "Email invalide" });

                if (string.IsNullOrWhiteSpace(dto.MotDePasse) || dto.MotDePasse.Length < 6)
                    return BadRequest(new { message = "Le mot de passe doit contenir au moins 6 caractères" });

                var client = await _authRepository.RegisterAsync(dto);

                if (client == null)
                    return StatusCode(500, new { message = "Erreur lors de l'inscription" });

                var token = _jwtService.GenerateToken(client.Id, client.Email, client.Nom, client.Prenom);
                var expiresAt = DateTime.UtcNow.AddHours(24);

                _logger.LogInformation("Nouveau client inscrit: {Email}", client.Email);

                return CreatedAtAction(nameof(GetProfile), new AuthResponseDto
                {
                    Token = token,
                    ExpiresAt = expiresAt,
                    Client = client
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'inscription");
                return StatusCode(500, new { message = "Erreur interne du serveur", details = ex.Message });
            }
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
        {
            try
            {
                var client = await _authRepository.LoginAsync(dto);

                if (client == null)
                    return Unauthorized(new { message = "Email ou mot de passe incorrect" });

                var token = _jwtService.GenerateToken(client.Id, client.Email, client.Nom, client.Prenom);
                var expiresAt = DateTime.UtcNow.AddHours(24);

                _logger.LogInformation("Client connecté: {Email}", client.Email);

                return Ok(new AuthResponseDto
                {
                    Token = token,
                    ExpiresAt = expiresAt,
                    Client = client
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la connexion");
                return StatusCode(500, new { message = "Erreur interne du serveur", details = ex.Message });
            }
        }

        [HttpGet("profile")]
        [Authorize]
        [ProducesResponseType(typeof(ClientDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ClientDto>> GetProfile()
        {
            try
            {
                var clientId = _jwtService.GetClientIdFromToken(User);

                if (clientId == null)
                    return Unauthorized(new { message = "Token invalide" });

                var client = await _authRepository.GetClientByIdAsync(clientId.Value);

                if (client == null)
                    return NotFound(new { message = "Client introuvable" });

                return Ok(client);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du profil");
                return StatusCode(500, new { message = "Erreur interne du serveur", details = ex.Message });
            }
        }
    }
}