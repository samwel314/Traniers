using ERP.Application.Common.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace ERP.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true)
    {
        var emailSettings =
            _configuration.GetSection("EmailSettings");
        
        using var message = new MailMessage();

        message.From = new MailAddress(
            emailSettings["From"]!);

        message.To.Add(to);

        message.Subject = subject;

        message.Body = body;

        message.IsBodyHtml = isHtml;

        using var smtp = new SmtpClient(
            emailSettings["Host"]!,
            int.Parse(emailSettings["Port"]!));

        smtp.Credentials = new NetworkCredential(
            emailSettings["Username"]!,
            emailSettings["Password"]!);

        smtp.EnableSsl = true;

        await smtp.SendMailAsync(message);
    }
}