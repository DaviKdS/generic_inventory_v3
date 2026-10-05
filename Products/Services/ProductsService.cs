using Microsoft.EntityFrameworkCore;
using GenericInventory.Data;
using GenericInventory.Products.Dtos;
using GenericInventory.Products.Entities;

namespace GenericInventory.Products.Services;

public class ProductsService
{
    private readonly AppDbContext _db;

    public ProductsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ProductDto>> GetAsync(
        string? search,
        bool criticalOnly,
        string? category,
        bool favoriteOnly,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        var normalizedUserId = NormalizeUserId(userId);
        var favoriteProductIds = normalizedUserId.Length == 0
            ? new HashSet<int>()
            : (await _db.ProductFavorites
                .AsNoTracking()
                .Where(favorite => favorite.UserId == normalizedUserId)
                .Select(favorite => favorite.ProductId)
                .ToListAsync(cancellationToken))
                .ToHashSet();

        if (favoriteOnly && favoriteProductIds.Count == 0)
        {
            return [];
        }

        var query = _db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(product =>
                product.Code.Contains(term) ||
                product.Description.Contains(term) ||
                product.Category.Contains(term) ||
                product.Catalyst.Contains(term));
        }

        if (criticalOnly)
        {
            query = query.Where(product => product.CurrentStock <= product.MinimumStock);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalizedCategory = ProductCategories.Normalize(category);
            query = query.Where(product => product.Category == normalizedCategory);
        }

        if (favoriteOnly)
        {
            query = query.Where(product => favoriteProductIds.Contains(product.Id));
        }

        var products = await query.ToListAsync(cancellationToken);
        return products
            .OrderBy(product => product.Description)
            .Select(product => ProductDto.FromEntity(product, favoriteProductIds.Contains(product.Id)))
            .ToList();
    }

    public async Task<ProductDto> CreateAsync(ProductFormDto form, string? userId, CancellationToken cancellationToken = default)
    {
        var code = NormalizeCode(form.Code);
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("Informe o codigo do produto.");
        }

        if (string.IsNullOrWhiteSpace(form.Description))
        {
            throw new InvalidOperationException("Informe a descricao do produto.");
        }

        if (await _db.Products.AnyAsync(product => product.Code == code, cancellationToken))
        {
            throw new InvalidOperationException("Ja existe um produto com este codigo.");
        }

        var product = new Product();
        Apply(product, form, code);

        _db.Products.Add(product);
        await _db.SaveChangesAsync(cancellationToken);
        return ProductDto.FromEntity(product);
    }

    public async Task<ProductDto> UpdateAsync(string code, ProductFormDto form, string? userId, CancellationToken cancellationToken = default)
    {
        var product = await FindByCodeAsync(code, cancellationToken);
        var normalizedUserId = NormalizeUserId(userId);
        var nextCode = string.IsNullOrWhiteSpace(form.Code) ? product.Code : NormalizeCode(form.Code);

        if (!string.Equals(product.Code, nextCode, StringComparison.OrdinalIgnoreCase) &&
            await _db.Products.AnyAsync(item => item.Code == nextCode, cancellationToken))
        {
            throw new InvalidOperationException("Ja existe um produto com este codigo.");
        }

        Apply(product, form, nextCode);
        await _db.SaveChangesAsync(cancellationToken);
        var isFavorite = normalizedUserId.Length > 0 && await _db.ProductFavorites.AnyAsync(
            favorite => favorite.ProductId == product.Id && favorite.UserId == normalizedUserId,
            cancellationToken);
        return ProductDto.FromEntity(product, isFavorite);
    }

    public async Task<ProductDto> SetFavoriteAsync(
        string code,
        bool isFavorite,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        var normalizedUserId = NormalizeUserId(userId);
        if (normalizedUserId.Length == 0)
        {
            throw new InvalidOperationException("Usuario autenticado nao identificado.");
        }

        var product = await FindByCodeAsync(code, cancellationToken);
        var favorite = await _db.ProductFavorites.FirstOrDefaultAsync(
            item => item.ProductId == product.Id && item.UserId == normalizedUserId,
            cancellationToken);

        if (isFavorite && favorite == null)
        {
            _db.ProductFavorites.Add(new ProductFavorite
            {
                ProductId = product.Id,
                UserId = normalizedUserId
            });
        }
        else if (!isFavorite && favorite != null)
        {
            _db.ProductFavorites.Remove(favorite);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ProductDto.FromEntity(product, isFavorite);
    }

    public async Task DeleteAsync(string code, CancellationToken cancellationToken = default)
    {
        var product = await FindByCodeAsync(code, cancellationToken);
        _db.Products.Remove(product);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Product> FindByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var normalized = NormalizeCode(code);
        return await _db.Products.FirstOrDefaultAsync(product => product.Code == normalized, cancellationToken)
            ?? throw new KeyNotFoundException("Produto nao encontrado.");
    }

    private static void Apply(Product product, ProductFormDto form, string code)
    {
        product.Code = code;
        product.Description = form.Description.Trim();
        product.Category = ProductCategories.Normalize(form.Category);
        product.CurrentStock = form.CurrentStock;
        product.MinimumStock = Math.Round(form.MinimumStock, 0, MidpointRounding.AwayFromZero);
        product.SaleValue = form.SaleValue;
        product.ImagePath = form.ImagePath.Trim();
        product.LegacyImageUrl = form.LegacyImageUrl.Trim();
        product.Catalyst = form.Catalyst.Trim();
    }

    private static string NormalizeCode(string code)
    {
        return code.Trim();
    }

    private static string NormalizeUserId(string? userId)
    {
        return (userId ?? string.Empty).Trim();
    }
}
