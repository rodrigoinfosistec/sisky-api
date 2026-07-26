namespace SiskyApi.Shared.Authorization;

public record ModuleConfig(
    string Slug,
    string Name,
    string Description,
    string[] Actions,
    bool IsCore = false
);

public static class PermissionsConfig
{
    public static readonly ModuleConfig[] Modules = new[]
    {
        // Core — always active
        new ModuleConfig("users", "Usuários", "Gestão de usuários e permissões",
            new[] { "view", "create", "edit", "delete" }, IsCore: true),

        new ModuleConfig("audit", "Auditoria", "Logs de auditoria",
            new[] { "view" }, IsCore: true),

        // Optional
        new ModuleConfig("inventory", "Estoque", "Gestão de produtos e estoque",
            new[] { "view", "create", "edit", "delete" }),

        new ModuleConfig("fiscal", "Fiscal", "Gestão de notas fiscais",
            new[] { "view", "create", "edit", "delete" }),

        new ModuleConfig("timeclock", "Ponto", "Gestão de ponto e jornada de trabalho",
            new[] { "view", "create", "edit", "delete" }),
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