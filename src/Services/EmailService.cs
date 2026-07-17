using Resend;

namespace SiskyApi.Services;

public class EmailService
{
    private readonly IResend _resend;
    private readonly string _fromAddress;
    private readonly string _fromName;

    public EmailService(IResend resend, IConfiguration configuration)
    {
        _resend = resend;
        _fromAddress = configuration["Mail:FromAddress"]!;
        _fromName = configuration["Mail:FromName"]!;
    }

    public async Task SendPasswordResetAsync(string toEmail, string toName, string resetLink)
    {
        var template = await File.ReadAllTextAsync("Templates/password-reset.html");

        var html = template
            .Replace("{{toName}}", toName)
            .Replace("{{resetLink}}", resetLink)
            .Replace("{{year}}", DateTime.UtcNow.Year.ToString());

        var message = new EmailMessage
        {
            From = $"{_fromName} <{_fromAddress}>",
            To = { toEmail },
            Subject = "Recuperação de senha",
            HtmlBody = html
        };

        await _resend.EmailSendAsync(message);
    }
}