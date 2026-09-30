namespace SIRGA.Application.Interfaces;

public interface IEmailService
{
  Task SendEmailAsync(string to, string subject, string body, byte[]? attachment = null, string? attachmentName = null);
}
