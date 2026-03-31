using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace CloudsferQA.Services;

public sealed class SmtpSettings
{
    public string Host      { get; set; } = string.Empty;
    public int    Port      { get; set; } = 587;
    public string Username  { get; set; } = string.Empty;
    public string Password  { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "noreply@tzunami.com";
    public string FromName  { get; set; } = "CloudsferQA";
}

public class EmailService
{
    private readonly SmtpSettings          _smtp;
    private readonly ILogger<EmailService> _log;

    public EmailService(IConfiguration config, ILogger<EmailService> log)
    {
        _smtp = config.GetSection("Smtp").Get<SmtpSettings>() ?? new SmtpSettings();
        _log  = log;
    }

    public async Task SendVerificationEmailAsync(string toEmail, string verifyUrl)
    {
        var subject = "CloudsferQA — Verify your email address";
        var body = $"""
            <div style="font-family:Inter,Arial,sans-serif;max-width:520px;margin:0 auto;padding:0 20px">
              <div style="background:linear-gradient(135deg,#0D47A1,#1565C0);padding:24px 28px;border-radius:12px 12px 0 0">
                <h2 style="color:#fff;margin:0;font-size:20px">CloudsferQA — Email Verification</h2>
              </div>
              <div style="background:#fff;padding:28px;border:1px solid #E0E0E0;border-top:none;border-radius:0 0 12px 12px">
                <p style="color:#212529">Hi,</p>
                <p style="color:#546E7A">You registered for <strong>CloudsferQA</strong> with this email address.
                   Click the button below to verify your account and start testing:</p>
                <div style="text-align:center;margin:32px 0">
                  <a href="{verifyUrl}"
                     style="background:#1565C0;color:#fff;padding:14px 32px;border-radius:8px;
                            text-decoration:none;font-weight:600;font-size:15px;display:inline-block">
                    ✓ Verify Email Address
                  </a>
                </div>
                <p style="color:#90A4AE;font-size:12px">Or paste this link in your browser:<br>
                   <a href="{verifyUrl}" style="color:#1565C0">{verifyUrl}</a></p>
                <hr style="border:none;border-top:1px solid #ECEFF1;margin:24px 0">
                <p style="color:#90A4AE;font-size:12px;margin:0">
                  This link is valid for 24 hours. If you did not register, please ignore this email.
                </p>
              </div>
              <p style="color:#B0BEC5;font-size:11px;text-align:center;margin-top:16px">
                Cloudsfer QA Portal · Internal Use Only
              </p>
            </div>
            """;

        await SendAsync(toEmail, subject, body);
    }

    private async Task SendAsync(string to, string subject, string htmlBody)
    {
        // Dev mode: if SMTP host is not configured, log to console and return
        if (string.IsNullOrWhiteSpace(_smtp.Host))
        {
            _log.LogWarning("SMTP not configured — email NOT sent to {To}. Subject: {Subject}", to, subject);
            _log.LogInformation("[DEV] Verification email would be sent to {To}", to);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_smtp.Username, _smtp.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(quit: true);
    }
}
