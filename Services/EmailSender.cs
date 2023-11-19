using Microsoft.AspNetCore.Identity.UI.Services;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace IdentityManager.Services
{
    public class EmailSender : IEmailSender
    {
        public string? SendGridKey { get; set; }
        public EmailSender(IConfiguration config)
        {
            SendGridKey = config.GetValue<string>("SendGrid:SecretKey");
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var client = new SendGridClient(SendGridKey);
            var from = new EmailAddress("noreply@rommeldetorres.me", "DetorresAcademy - Identity Manager");
            var to = new EmailAddress(email);
            
            var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlMessage);
            return client.SendEmailAsync(msg);
        }
    }
}