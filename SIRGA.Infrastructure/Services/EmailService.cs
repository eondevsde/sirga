using SIRGA.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace SIRGA.Infrastructure.Services;

public class EmailService(IConfiguration configuration) : IEmailService
{
  public async Task SendEmailAsync(string to, string subject, string body, byte[]? attachment = null, string? attachmentName = null)
  {
    var smtpServer = configuration["Email:SmtpServer"] ?? "localhost";
    var smtpPort = int.Parse(configuration["Email:SmtpPort"] ?? "25");
    var fromEmail = configuration["Email:FromEmail"] ?? "no-reply@sirga.com";
    var password = configuration["Email:Password"];

    using var client = new SmtpClient(smtpServer, smtpPort);
    client.EnableSsl = true;
    if (!string.IsNullOrEmpty(password))
    {
      client.Credentials = new NetworkCredential(fromEmail, password);
    }

    var mailMessage = new MailMessage
    {
      From = new MailAddress(fromEmail),
      Subject = subject,
      Body = body,
      IsBodyHtml = true,
    };
    mailMessage.To.Add(to);

    if (attachment != null && !string.IsNullOrEmpty(attachmentName))
    {
      mailMessage.Attachments.Add(new Attachment(new MemoryStream(attachment), attachmentName));
    }

    await client.SendMailAsync(mailMessage);
  }
}
