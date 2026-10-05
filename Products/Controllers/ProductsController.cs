using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using GenericInventory.Auth.AccessControl;
using GenericInventory.Products.Dtos;
using GenericInventory.Products.Services;

namespace GenericInventory.Products.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".gif"
    };

    private readonly ProductsService _service;
    private readonly IWebHostEnvironment _environment;

    public ProductsController(ProductsService service, IWebHostEnvironment environment)
    {
        _service = service;
        _environment = environment;
    }

    [Authorize(Policy = AccessPermissions.StockRead)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> Get(
        [FromQuery] string? search,
        [FromQuery] bool criticalOnly,
        [FromQuery] string? category,
        [FromQuery] bool favoriteOnly,
        CancellationToken cancellationToken)
    {
        return Ok(await _service.GetAsync(search, criticalOnly, category, favoriteOnly, CurrentUserId(), cancellationToken));
    }

    [Authorize(Policy = AccessPermissions.ProductsManage)]
    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create([FromBody] ProductFormDto form, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(() => _service.CreateAsync(form, CurrentUserId(), cancellationToken));
    }

    [Authorize(Policy = AccessPermissions.ProductsManage)]
    [HttpPut("{code}")]
    public async Task<ActionResult<ProductDto>> Update(string code, [FromBody] ProductFormDto form, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(() => _service.UpdateAsync(code, form, CurrentUserId(), cancellationToken));
    }

    [Authorize(Policy = AccessPermissions.StockRead)]
    [HttpPut("{code}/favorite")]
    public async Task<ActionResult<ProductDto>> SetFavorite(
        string code,
        [FromBody] ProductFavoriteRequestDto request,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(() => _service.SetFavoriteAsync(code, request.IsFavorite, CurrentUserId(), cancellationToken));
    }

    [Authorize(Policy = AccessPermissions.ProductsManage)]
    [HttpPost("image")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Selecione uma imagem." });
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            return Conflict(new { message = "A imagem deve ter no maximo 5 MB." });
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedImageExtensions.Contains(extension) ||
            !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new { message = "Envie uma imagem JPG, PNG, WEBP ou GIF." });
        }

        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var uploadDirectory = Path.Combine(webRoot, "assets", "products", "uploads");
        Directory.CreateDirectory(uploadDirectory);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var fullPath = Path.Combine(uploadDirectory, fileName);
        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return Ok(new { imagePath = $"/assets/products/uploads/{fileName}" });
    }

    [Authorize(Policy = AccessPermissions.ProductsManage)]
    [HttpDelete("{code}")]
    public async Task<IActionResult> Delete(string code, CancellationToken cancellationToken)
    {
        try
        {
            await _service.DeleteAsync(code, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private async Task<ActionResult<ProductDto>> ExecuteAsync(Func<Task<ProductDto>> action)
    {
        try
        {
            return Ok(await action());
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    private string CurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    }
}
