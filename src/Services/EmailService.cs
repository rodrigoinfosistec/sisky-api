using Resend;

namespace SiskyApi.Services;

public class EmailService
{
    private readonly IResend _resend;
    private readonly string _fromAddress;
    private readonly string _fromName;
    private readonly string _domain;

    public EmailService(IResend resend, IConfiguration configuration)
    {
        _resend = resend;
        _fromAddress = configuration["Mail:FromAddress"]!;
        _fromName = configuration["Mail:FromName"]!;
        _domain = configuration["App:Domain"]!;
    }

    private async Task<string> LoadTemplate(string templateName)
    {
        return await File.ReadAllTextAsync($"Templates/{templateName}");
    }

    private async Task SendAsync(string toEmail, string subject, string html)
    {
        var message = new EmailMessage
        {
            From = $"{_fromName} <{_fromAddress}>",
            To = { toEmail },
            Subject = subject,
            HtmlBody = html
        };

        await _resend.EmailSendAsync(message);
    }

    public async Task SendPasswordResetAsync(string toEmail, string toName, string resetLink)
    {
        var template = await LoadTemplate("password-reset.html");

        var html = template
            .Replace("{{toName}}", toName)
            .Replace("{{resetLink}}", resetLink)
            .Replace("{{year}}", DateTime.UtcNow.Year.ToString());

        await SendAsync(toEmail, "Recuperação de senha", html);
    }

    public async Task SendTicketOpenedToTenantAsync(
        string toEmail,
        string toName,
        int ticketId,
        string ticketTitle,
        string ticketPriority,
        string companyName,
        string subdomain)
    {
        var template = await LoadTemplate("ticket-opened.html");
        var ticketUrl = $"https://{subdomain}.{_domain}/support/{ticketId}";

        var priorityLabels = new Dictionary<string, string>
        {
            { "low", "Baixa" },
            { "medium", "Média" },
            { "high", "Alta" },
            { "urgent", "Urgente" }
        };

        var html = template
            .Replace("{{toName}}", toName)
            .Replace("{{ticketId}}", ticketId.ToString())
            .Replace("{{ticketTitle}}", ticketTitle)
            .Replace("{{ticketPriority}}", priorityLabels.GetValueOrDefault(ticketPriority, ticketPriority))
            .Replace("{{companyName}}", companyName)
            .Replace("{{ticketUrl}}", ticketUrl)
            .Replace("{{message}}", "Seu ticket foi aberto com sucesso. Nossa equipe de suporte irá analisá-lo em breve.")
            .Replace("{{year}}", DateTime.UtcNow.Year.ToString());

        await SendAsync(toEmail, $"Ticket #{ticketId} aberto com sucesso", html);
    }

    public async Task SendTicketOpenedToAdminAsync(
        string supportEmail,
        int ticketId,
        string ticketTitle,
        string ticketPriority,
        string companyName,
        string tenantName,
        string userName)
    {
        var template = await LoadTemplate("ticket-opened.html");
        var ticketUrl = $"https://admin.{_domain}/tickets/{ticketId}";

        var priorityLabels = new Dictionary<string, string>
        {
            { "low", "Baixa" },
            { "medium", "Média" },
            { "high", "Alta" },
            { "urgent", "Urgente" }
        };

        var html = template
            .Replace("{{toName}}", "Administrador")
            .Replace("{{ticketId}}", ticketId.ToString())
            .Replace("{{ticketTitle}}", ticketTitle)
            .Replace("{{ticketPriority}}", priorityLabels.GetValueOrDefault(ticketPriority, ticketPriority))
            .Replace("{{companyName}}", $"{companyName} ({tenantName})")
            .Replace("{{ticketUrl}}", ticketUrl)
            .Replace("{{message}}", $"{userName} abriu um novo ticket de suporte.")
            .Replace("{{year}}", DateTime.UtcNow.Year.ToString());

        await SendAsync(supportEmail, $"Novo ticket #{ticketId} — {ticketTitle}", html);
    }

    public async Task SendTicketReplyToTenantAsync(
        string toEmail,
        string toName,
        int ticketId,
        string ticketTitle,
        string replyMessage,
        string subdomain)
    {
        var template = await LoadTemplate("ticket-reply.html");
        var ticketUrl = $"https://{subdomain}.{_domain}/support/{ticketId}";

        var html = template
            .Replace("{{toName}}", toName)
            .Replace("{{ticketId}}", ticketId.ToString())
            .Replace("{{ticketTitle}}", ticketTitle)
            .Replace("{{senderName}}", "Equipe de Suporte")
            .Replace("{{replyMessage}}", replyMessage)
            .Replace("{{ticketUrl}}", ticketUrl)
            .Replace("{{year}}", DateTime.UtcNow.Year.ToString());

        await SendAsync(toEmail, $"Nova resposta no ticket #{ticketId}", html);
    }

    public async Task SendTicketReplyToAdminAsync(
        string supportEmail,
        int ticketId,
        string ticketTitle,
        string replyMessage,
        string userName)
    {
        var template = await LoadTemplate("ticket-reply.html");
        var ticketUrl = $"https://admin.{_domain}/tickets/{ticketId}";

        var html = template
            .Replace("{{toName}}", "Administrador")
            .Replace("{{ticketId}}", ticketId.ToString())
            .Replace("{{ticketTitle}}", ticketTitle)
            .Replace("{{senderName}}", userName)
            .Replace("{{replyMessage}}", replyMessage)
            .Replace("{{ticketUrl}}", ticketUrl)
            .Replace("{{year}}", DateTime.UtcNow.Year.ToString());

        await SendAsync(supportEmail, $"Nova resposta no ticket #{ticketId} — {userName}", html);
    }

    public async Task SendTicketStatusChangedAsync(
        string toEmail,
        string toName,
        int ticketId,
        string ticketTitle,
        string oldStatus,
        string newStatus,
        string subdomain)
    {
        var template = await LoadTemplate("ticket-status-changed.html");
        var ticketUrl = $"https://{subdomain}.{_domain}/support/{ticketId}";

        var statusLabels = new Dictionary<string, string>
        {
            { "open", "Aberto" },
            { "in_progress", "Em andamento" },
            { "resolved", "Resolvido" },
            { "closed", "Fechado" }
        };

        var html = template
            .Replace("{{toName}}", toName)
            .Replace("{{ticketId}}", ticketId.ToString())
            .Replace("{{ticketTitle}}", ticketTitle)
            .Replace("{{oldStatus}}", statusLabels.GetValueOrDefault(oldStatus, oldStatus))
            .Replace("{{newStatus}}", statusLabels.GetValueOrDefault(newStatus, newStatus))
            .Replace("{{ticketUrl}}", ticketUrl)
            .Replace("{{year}}", DateTime.UtcNow.Year.ToString());

        await SendAsync(toEmail, $"Status do ticket #{ticketId} atualizado", html);
    }
}