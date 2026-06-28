namespace SiskyApi.Authorization;

public record ModuleConfig(
    string Slug,
    string Name,
    string Description,
    string[] Actions
);

public static class PermissionsConfig
{
    public static readonly ModuleConfig[] Modules = new[]
    {
        new ModuleConfig("users", "Usuários", "Gestão de usuários e permissões",
            new[] { "view", "create", "edit", "delete" }),

        new ModuleConfig("financeiro", "Financeiro", "Gestão financeira",
            new[] { "view", "create", "edit", "delete" }),

        new ModuleConfig("rh", "RH", "Recursos humanos",
            new[] { "view", "create", "edit", "delete" }),

        new ModuleConfig("crm", "CRM", "Gestão de clientes",
            new[] { "view", "create", "edit", "delete" }),

        new ModuleConfig("audit", "Auditoria", "Logs de auditoria",
            new[] { "view" }),
    };

    public static IEnumerable<string> All =>
        Modules.SelectMany(m => m.Actions.Select(a => $"{m.Slug}.{a}"));

    public static string DescriptionFor(string slug)
    {
        var parts = slug.Split('.');
        if (parts.Length != 2) return slug;

        var module = Modules.FirstOrDefault(m => m.Slug == parts[0]);
        if (module is null) return slug;

        return parts[1] switch
        {
            "view" => $"Visualizar {module.Name}",
            "create" => $"Criar em {module.Name}",
            "edit" => $"Editar em {module.Name}",
            "delete" => $"Excluir em {module.Name}",
            _ => slug
        };
    }
}