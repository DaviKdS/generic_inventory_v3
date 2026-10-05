namespace GenericInventory.Products.Entities;

public static class ProductCategories
{
    public const string General = "Geral";
    public const string Materials = "Materiais";
    public const string Supplies = "Insumos";
    public const string Tools = "Ferramentas";
    public const string Equipment = "Equipamentos";
    public const string Others = "Outros";

    public const string Default = General;

    public static readonly IReadOnlyList<string> All =
    [
        General,
        Materials,
        Supplies,
        Tools,
        Equipment,
        Others
    ];

    public static string Normalize(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return Default;
        }

        return All.FirstOrDefault(item => string.Equals(item, category.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? Default;
    }
}
