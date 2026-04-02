using Microsoft.Data.SqlClient;
using DotnetLocationRest.DTOs;

namespace DotnetLocationRest.Data
{
    public class VehiculeRepository
    {
        private readonly DbConnectionFactory _dbFactory;

        public VehiculeRepository(DbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<PagedResult<VehiculeListDto>> GetVehiculesAsync(VehiculeFilterDto filter)
        {
            var result = new PagedResult<VehiculeListDto>
            {
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

            using var connection = _dbFactory.Create();
            await connection.OpenAsync();

            var whereConditions = new List<string>();
            var parameters = new List<SqlParameter>();

            if (filter.CategorieId.HasValue)
            {
                whereConditions.Add("v.CategorieId = @CategorieId");
                parameters.Add(new SqlParameter("@CategorieId", filter.CategorieId.Value));
            }

            if (filter.PrixMin.HasValue)
            {
                whereConditions.Add("v.Prix >= @PrixMin");
                parameters.Add(new SqlParameter("@PrixMin", filter.PrixMin.Value));
            }

            if (filter.PrixMax.HasValue)
            {
                whereConditions.Add("v.Prix <= @PrixMax");
                parameters.Add(new SqlParameter("@PrixMax", filter.PrixMax.Value));
            }

            if (filter.Disponible.HasValue)
            {
                whereConditions.Add("v.Disponibilite = @Disponible");
                parameters.Add(new SqlParameter("@Disponible", filter.Disponible.Value));
            }

            if (!string.IsNullOrWhiteSpace(filter.Transmission))
            {
                whereConditions.Add("v.Transmission = @Transmission");
                parameters.Add(new SqlParameter("@Transmission", filter.Transmission));
            }

            if (!string.IsNullOrWhiteSpace(filter.Carburant))
            {
                whereConditions.Add("v.Carburant = @Carburant");
                parameters.Add(new SqlParameter("@Carburant", filter.Carburant));
            }

            if (!string.IsNullOrWhiteSpace(filter.Marque))
            {
                whereConditions.Add("v.Marque LIKE @Marque");
                parameters.Add(new SqlParameter("@Marque", $"%{filter.Marque}%"));
            }

            var whereClause = whereConditions.Any() 
                ? "WHERE " + string.Join(" AND ", whereConditions) 
                : "";

            var orderByColumn = filter.SortBy?.ToLower() switch
            {
                "marque" => "v.Marque",
                "annee" => "v.Annee",
                "prix" => "v.Prix",
                _ => "v.Prix"
            };

            var orderDirection = filter.SortOrder?.ToLower() == "desc" ? "DESC" : "ASC";

            var countQuery = $@"
                SELECT COUNT(*) 
                FROM Vehicule v
                INNER JOIN Categorie c ON v.CategorieId = c.Id
                {whereClause}";

            using (var countCommand = new SqlCommand(countQuery, connection))
            {
                foreach (var param in parameters)
                {
                    countCommand.Parameters.Add(new SqlParameter(param.ParameterName, param.Value));
                }
                result.TotalCount = (int)await countCommand.ExecuteScalarAsync();
            }

            var offset = (filter.PageNumber - 1) * filter.PageSize;

            var query = $@"
                SELECT 
                    v.Id,
                    v.Marque,
                    v.Model,
                    v.Annee,
                    v.Transmission,
                    v.Carburant,
                    v.Prix,
                    v.Disponibilite,
                    v.CategorieId,
                    c.Nom AS CategorieName,
                    (SELECT TOP 1 ImageUrl FROM VehiculeImage WHERE VehiculeId = v.Id AND EstPrincipale = 1) AS ImagePrincipale
                FROM Vehicule v
                INNER JOIN Categorie c ON v.CategorieId = c.Id
                {whereClause}
                ORDER BY {orderByColumn} {orderDirection}
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            using (var command = new SqlCommand(query, connection))
            {
                foreach (var param in parameters)
                {
                    command.Parameters.Add(new SqlParameter(param.ParameterName, param.Value));
                }
                command.Parameters.Add(new SqlParameter("@Offset", offset));
                command.Parameters.Add(new SqlParameter("@PageSize", filter.PageSize));

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Items.Add(new VehiculeListDto
                    {
                        Id = reader.GetInt32(0),
                        Marque = reader.GetString(1),
                        Model = reader.GetString(2),
                        Annee = reader.GetInt32(3),
                        Transmission = reader.GetString(4),
                        Carburant = reader.GetString(5),
                        Prix = reader.GetDecimal(6),
                        Disponibilite = reader.GetBoolean(7),
                        CategorieId = reader.GetInt32(8),
                        CategorieName = reader.GetString(9),
                        ImagePrincipale = reader.IsDBNull(10) ? null : reader.GetString(10)
                    });
                }
            }

            return result;
        }

        public async Task<VehiculeDetailDto?> GetVehiculeByIdAsync(int id)
        {
            using var connection = _dbFactory.Create();
            await connection.OpenAsync();

            var query = @"
                SELECT 
                    v.Id, v.Marque, v.Model, v.Annee, v.Transmission, v.Carburant,
                    v.Prix, v.Caution, v.Penalite, v.Disponibilite, v.CategorieId,
                    c.Nom AS CategorieName, c.Description AS CategorieDescription
                FROM Vehicule v
                INNER JOIN Categorie c ON v.CategorieId = c.Id
                WHERE v.Id = @Id";

            VehiculeDetailDto? vehicule = null;

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.Add(new SqlParameter("@Id", id));

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    vehicule = new VehiculeDetailDto
                    {
                        Id = reader.GetInt32(0),
                        Marque = reader.GetString(1),
                        Model = reader.GetString(2),
                        Annee = reader.GetInt32(3),
                        Transmission = reader.GetString(4),
                        Carburant = reader.GetString(5),
                        Prix = reader.GetDecimal(6),
                        Caution = reader.GetDecimal(7),
                        Penalite = reader.GetDecimal(8),
                        Disponibilite = reader.GetBoolean(9),
                        CategorieId = reader.GetInt32(10),
                        CategorieName = reader.GetString(11),
                        CategorieDescription = reader.IsDBNull(12) ? null : reader.GetString(12)
                    };
                }
            }

            if (vehicule != null)
            {
                var imageQuery = @"
                    SELECT Id, ImageUrl, EstPrincipale
                    FROM VehiculeImage
                    WHERE VehiculeId = @VehiculeId
                    ORDER BY EstPrincipale DESC, Id";

                using var imageCommand = new SqlCommand(imageQuery, connection);
                imageCommand.Parameters.Add(new SqlParameter("@VehiculeId", id));

                using var imageReader = await imageCommand.ExecuteReaderAsync();
                while (await imageReader.ReadAsync())
                {
                    vehicule.Images.Add(new ImageDto
                    {
                        Id = imageReader.GetInt32(0),
                        ImageUrl = imageReader.GetString(1),
                        EstPrincipale = imageReader.GetBoolean(2)
                    });
                }
            }

            return vehicule;
        }

        public async Task<List<CategorieDto>> GetCategoriesAsync()
        {
            var categories = new List<CategorieDto>();

            using var connection = _dbFactory.Create();
            await connection.OpenAsync();

            var query = @"
                SELECT c.Id, c.Nom, c.Description, COUNT(v.Id) AS NombreVehicules
                FROM Categorie c
                LEFT JOIN Vehicule v ON c.Id = v.CategorieId
                GROUP BY c.Id, c.Nom, c.Description
                ORDER BY c.Nom";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                categories.Add(new CategorieDto
                {
                    Id = reader.GetInt32(0),
                    Nom = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                    NombreVehicules = reader.GetInt32(3)
                });
            }

            return categories;
        }

        public async Task<Dictionary<string, List<string>>> GetFilterOptionsAsync()
        {
            var options = new Dictionary<string, List<string>>
            {
                ["Transmissions"] = new List<string>(),
                ["Carburants"] = new List<string>(),
                ["Marques"] = new List<string>()
            };

            using var connection = _dbFactory.Create();
            await connection.OpenAsync();

            var transmissionQuery = "SELECT DISTINCT Transmission FROM Vehicule ORDER BY Transmission";
            using (var command = new SqlCommand(transmissionQuery, connection))
            {
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    options["Transmissions"].Add(reader.GetString(0));
                }
            }

            var carburantQuery = "SELECT DISTINCT Carburant FROM Vehicule ORDER BY Carburant";
            using (var command = new SqlCommand(carburantQuery, connection))
            {
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    options["Carburants"].Add(reader.GetString(0));
                }
            }

            var marqueQuery = "SELECT DISTINCT Marque FROM Vehicule ORDER BY Marque";
            using (var command = new SqlCommand(marqueQuery, connection))
            {
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    options["Marques"].Add(reader.GetString(0));
                }
            }

            return options;
        }
    }
}