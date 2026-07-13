using System.Net;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CodeMaster.Application.Services.Auth;

public interface IEmailSender
{
    Task SendVerificationCodeAsync(string email, string code, string purpose);
}

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<EmailSender> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SendVerificationCodeAsync(string email, string code, string purpose)
    {
        var provider = _configuration["Email:Provider"] ?? "Console";
        var subject = "CodeMaster email verification code";
        var text = $"Your CodeMaster verification code is {code}. It expires in 10 minutes.";
        var html = $"""
            <div style="font-family:Arial,sans-serif;line-height:1.7;color:#111827">
              <h2>CodeMaster verification</h2>
              <p>Your verification code is:</p>
              <p style="font-size:28px;font-weight:700;letter-spacing:6px">{code}</p>
              <p>This code expires in 10 minutes. If you did not request it, ignore this email.</p>
            </div>
            """;

        if (provider.Equals("Resend", StringComparison.OrdinalIgnoreCase))
        {
            await SendByResendAsync(email, subject, text, html);
            return;
        }

        if (provider.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
        {
            await SendBySmtpAsync(email, subject, text, html);
            return;
        }

        _logger.LogInformation("CodeMaster verification code for {Email}, purpose {Purpose}: {Code}", email, purpose, code);
    }

    private async Task SendByResendAsync(string email, string subject, string text, string html)
    {
        var apiKey = _configuration["Email:Resend:ApiKey"];
        var from = _configuration["Email:From"] ?? _configuration["Email:Resend:From"];
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(from))
        {
            throw new InvalidOperationException("Resend email sender requires Email:Resend:ApiKey and Email:From.");
        }

        var client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        request.Headers.Authorization = new global::System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new
        {
            from,
            to = new[] { email },
            subject,
            text,
            html
        });

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Resend email failed: {(int)response.StatusCode} {body}");
        }
    }

    private async Task SendBySmtpAsync(string email, string subject, string text, string html)
    {
        var host = _configuration["Email:Smtp:Host"];
        var from = _configuration["Email:From"] ?? _configuration["Email:Smtp:From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
        {
            throw new InvalidOperationException("SMTP email sender requires Email:Smtp:Host and Email:From.");
        }

        var port = _configuration.GetValue("Email:Smtp:Port", 587);
        var enableSsl = _configuration.GetValue("Email:Smtp:EnableSsl", true);
        var userName = _configuration["Email:Smtp:UserName"];
        var password = _configuration["Email:Smtp:Password"];

        using var message = new MailMessage
        {
            From = new MailAddress(from),
            Subject = subject,
            Body = text,
            IsBodyHtml = false
        };
        message.To.Add(email);
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(text, Encoding.UTF8, MediaTypeNames.Text.Plain));
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, Encoding.UTF8, MediaTypeNames.Text.Html));

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };

        if (!string.IsNullOrWhiteSpace(userName))
        {
            client.Credentials = new NetworkCredential(userName, password);
        }

        try
        {
            await client.SendMailAsync(message);
        }
        catch (SmtpException ex)
        {
            var detail = ex.InnerException?.Message ?? ex.Message;
            throw new InvalidOperationException($"SMTP email failed: {detail}", ex);
        }
        catch (Exception ex)
        {
            var detail = ex.InnerException?.Message ?? ex.Message;
            throw new InvalidOperationException($"SMTP email failed: {detail}", ex);
        }
    }
}
