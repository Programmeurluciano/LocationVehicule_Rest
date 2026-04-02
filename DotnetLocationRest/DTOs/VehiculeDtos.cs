namespace DotnetLocationRest.DTOs
{
    public class VehiculeListDto
    {
        public int Id { get; set; }
        public string Marque { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Annee { get; set; }
        public string Transmission { get; set; } = string.Empty;
        public string Carburant { get; set; } = string.Empty;
        public decimal Prix { get; set; }
        public bool Disponibilite { get; set; }
        public int CategorieId { get; set; }
        public string CategorieName { get; set; } = string.Empty;
        public string? ImagePrincipale { get; set; }
    }

    public class VehiculeDetailDto
    {
        public int Id { get; set; }
        public string Marque { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Annee { get; set; }
        public string Transmission { get; set; } = string.Empty;
        public string Carburant { get; set; } = string.Empty;
        public decimal Prix { get; set; }
        public decimal Caution { get; set; }
        public decimal Penalite { get; set; }
        public bool Disponibilite { get; set; }
        public int CategorieId { get; set; }
        public string CategorieName { get; set; } = string.Empty;
        public string? CategorieDescription { get; set; }
        public List<ImageDto> Images { get; set; } = new();
    }

    public class ImageDto
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool EstPrincipale { get; set; }
    }

    public class VehiculeFilterDto
    {
        public int? CategorieId { get; set; }
        public decimal? PrixMin { get; set; }
        public decimal? PrixMax { get; set; }
        public bool? Disponible { get; set; }
        public string? Transmission { get; set; }
        public string? Carburant { get; set; }
        public string? Marque { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; } = "Prix";
        public string? SortOrder { get; set; } = "asc";
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPrevious => PageNumber > 1;
        public bool HasNext => PageNumber < TotalPages;
    }

    public class CategorieDto
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int NombreVehicules { get; set; }
    }
}