using CybontrolX.Interfaces;
using CybontrolX.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;

    public EmailService(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }

    public async Task SendConfirmationEmail(string email, string code)
    {
        using (MailMessage mailMessage = new MailMessage())
        {
            mailMessage.From = new MailAddress(_emailSettings.Username);
            mailMessage.To.Add(email);
            mailMessage.Subject = "Код подтверждения";
            mailMessage.Body = $"Ваш код подтверждения: {code}. Он действителен в течение 15 минут.";
            mailMessage.IsBodyHtml = false;

            using (SmtpClient smtpClient = new SmtpClient(_emailSettings.Host))
            {
                smtpClient.Port = _emailSettings.Port;
                smtpClient.Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password);
                smtpClient.EnableSsl = _emailSettings.UseSSL;

                try
                {
                    await smtpClient.SendMailAsync(mailMessage);
                }
                catch (Exception ex)
                {
                    throw new ApplicationException("Ошибка при отправке письма.", ex);
                }
            }
        }
    }
}