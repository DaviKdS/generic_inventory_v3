using GenericInventory.Products.Entities;

namespace GenericInventory.Products.Dtos;

public class ProductDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = ProductCategories.Default;
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal SaleValue { get; set; }
    public decimal TotalStockValue => CurrentStock * SaleValue;
    public string ImagePath { get; set; } = string.Empty;
    public string LegacyImageUrl { get; set; } = string.Empty;
    public string Catalyst { get; set; } = string.Empty;
    public bool IsFavorite { get; set; }
    public bool IsCritical => CurrentStock <= MinimumStock;

    public static ProductDto FromEntity(Product product, bool isFavorite = false)
    {
        return new ProductDto
        {
            Id = product.Id,
            Code = product.Code,
            Description = product.Description,
            Category = ProductCategories.Normalize(product.Category),
            CurrentStock = product.CurrentStock,
            MinimumStock = product.MinimumStock,
            SaleValue = product.SaleValue,
            ImagePath = product.ImagePath,
            LegacyImageUrl = product.LegacyImageUrl,
            Catalyst = product.Catalyst,
            IsFavorite = isFavorite
        };
    }
}
