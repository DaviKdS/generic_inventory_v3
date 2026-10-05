namespace GenericInventory.Products.Entities;

public class ProductFavorite
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
}
