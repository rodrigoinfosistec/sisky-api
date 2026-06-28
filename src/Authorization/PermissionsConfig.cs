namespace SiskyApi.Authorization;

public static class PermissionsConfig
{
    public static readonly string[] All = new[]
    {
        // Módulo Users
        "users.view",
        "users.create",
        "users.edit",
        "users.delete",

        // Módulo Financeiro
        "financeiro.view",
        "financeiro.create",
        "financeiro.edit",
        "financeiro.delete",

        // Módulo RH
        "rh.view",
        "rh.create",
        "rh.edit",
        "rh.delete",

        // Módulo CRM
        "crm.view",
        "crm.create",
        "crm.edit",
        "crm.delete",
    };
}