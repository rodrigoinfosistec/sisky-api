using Resend;

namespace SiskyApi.Services;

public class EmailService
{
    private readonly IResend _resend;
    private readonly string _fromAddress;
    private readonly string _fromName;

    public EmailService(IConfiguration configuration)
    {
        var apiKey = configuration["Mail:ApiKey"]!;
        _fromAddress = configuration["Mail:FromAddress"]!;
        _fromName = configuration["Mail:FromName"]!;

        _resend = ResendClient.Create(new ResendClientOptions
        {
            ApiToken = apiKey
        });
    }

    public async Task SendPasswordResetAsync(string toEmail, string toName, string resetLink)
    {
        var message = new EmailMessage
        {
            From = $"{_fromName} <{_fromAddress}>",
            To = { toEmail },
            Subject = "Recuperação de senha",
            HtmlBody = $"""
                <div style="font-family: sans-serif; max-width: 600px; margin: 0 auto;">
                    <h2>Recuperação de senha</h2>
                    <p>Olá, {toName}!</p>
                    <p>Recebemos uma solicitação para redefinir a senha da sua conta.</p>
                    <p>Clique no botão abaixo para criar uma nova senha:</p>
                    <a href="{resetLink}" 
                       style="display: inline-block; padding: 12px 24px; background-color: #111; color: #fff; text-decoration: none; border-radius: 8px; margin: 16px 0;">
                        Redefinir senha
                    </a>
                    <p style="color: #666; font-size: 14px;">Este link expira em 1 hora.</p>
                    <p style="color: #666; font-size: 14px;">Se você não solicitou a recuperação de senha, ignore este e-mail.</p>
                </div>
            """
        };

        await _resend.EmailSendAsync(message);
    }
}