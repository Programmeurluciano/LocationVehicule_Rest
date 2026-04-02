using Microsoft.AspNetCore.Mvc;
using DotnetLocationRest.Data;
using DotnetLocationRest.DTOs;

namespace DotnetLocationRest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehiculesController : ControllerBase
    {
        private readonly VehiculeRepository _repository;
        private readonly ILogger<VehiculesController> _logger;

        public VehiculesController(VehiculeRepository repository, ILogger<VehiculesController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<VehiculeListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<VehiculeListDto>>> GetVehicules(
            [FromQuery] int? categorieId,
            [FromQuery] decimal? prixMin,
            [FromQuery] decimal? prixMax,
            [FromQuery] bool? disponible,
            [FromQuery] string? transmission,
            [FromQuery] string? carburant,
            [FromQuery] string? marque,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sortBy = "Prix",
            [FromQuery] string? sortOrder = "asc")
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 50) pageSize = 50;

                var filter = new VehiculeFilterDto
                {
                    CategorieId = categorieId,
                    PrixMin = prixMin,
                    PrixMax = prixMax,
                    Disponible = disponible,
                    Transmission = transmission,
                    Carburant = carburant,
                    Marque = marque,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    SortBy = sortBy,
                    SortOrder = sortOrder
                };

                var result = await _repository.GetVehiculesAsync(filter);

                _logger.LogInformation(
                    "Récupération de {Count} véhicules (page {PageNumber}/{TotalPages})",
                    result.Items.Count,
                    result.PageNumber,
                    result.TotalPages
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des véhicules");
                return StatusCode(500, new { message = "Erreur interne du serveur", details = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(VehiculeDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<VehiculeDetailDto>> GetVehicule(int id)
        {
            try
            {
                var vehicule = await _repository.GetVehiculeByIdAsync(id);

                if (vehicule == null)
                {
                    _logger.LogWarning("Véhicule avec ID {Id} introuvable", id);
                    return NotFound(new { message = $"Véhicule avec l'ID {id} introuvable" });
                }

                _logger.LogInformation("Détails du véhicule {Id} récupérés avec succès", id);
                return Ok(vehicule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du véhicule {Id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur", details = ex.Message });
            }
        }

        [HttpGet("categories")]
        [ProducesResponseType(typeof(List<CategorieDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CategorieDto>>> GetCategories()
        {
            try
            {
                var categories = await _repository.GetCategoriesAsync();
                _logger.LogInformation("Récupération de {Count} catégories", categories.Count);
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des catégories");
                return StatusCode(500, new { message = "Erreur interne du serveur", details = ex.Message });
            }
        }

        [HttpGet("filter-options")]
        [ProducesResponseType(typeof(Dictionary<string, List<string>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<Dictionary<string, List<string>>>> GetFilterOptions()
        {
            try
            {
                var options = await _repository.GetFilterOptionsAsync();
                _logger.LogInformation("Options de filtrage récupérées");
                return Ok(options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des options de filtrage");
                return StatusCode(500, new { message = "Erreur interne du serveur", details = ex.Message });
            }
        }

        [HttpGet("disponibles")]
        [ProducesResponseType(typeof(PagedResult<VehiculeListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<VehiculeListDto>>> GetVehiculesDisponibles(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var filter = new VehiculeFilterDto
                {
                    Disponible = true,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    SortBy = "Prix",
                    SortOrder = "asc"
                };

                var result = await _repository.GetVehiculesAsync(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des véhicules disponibles");
                return StatusCode(500, new { message = "Erreur interne du serveur", details = ex.Message });
            }
        }
    }
}