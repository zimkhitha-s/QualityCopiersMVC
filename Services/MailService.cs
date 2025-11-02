using Google.Cloud.SecretManager.V1;
using INSY7315_ElevateDigitalStudios_POE.Helper;
using MimeKit;

namespace INSY7315_ElevateDigitalStudios_POE.Services
{
    public class MailService
    {
        private readonly string _smtpServer = "smtp.gmail.com";
        private readonly int _smtpPort = 587;
        private readonly string _smtpUser;
        private readonly string _smtpPassword;
        private readonly string _managerEmail;

        public MailService()
        {
            // Retrieve your Google Cloud project ID
            var projectId = Environment.GetEnvironmentVariable("GCP_PROJECT_ID");

            // Create a Secret Manager client
            var client = SecretManagerServiceClient.Create();

            // Use centralized SecretManagerHelper
            _smtpUser = SecretManagerHelper.GetSecret(projectId, "email-smtp-user");
            _smtpPassword = SecretManagerHelper.GetSecret(projectId, "email-smtp-password");
            _managerEmail = SecretManagerHelper.GetSecret(projectId, "manager-email");
        }

        public string ManagerEmail => _managerEmail;

        public async Task SendEmailAsync(MimeMessage email)
        {
            using var smtp = new MailKit.Net.Smtp.SmtpClient();

            await smtp.ConnectAsync(_smtpServer, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_smtpUser, _smtpPassword);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
//-------------------------------------------------------------------------------------------End Of File--------------------------------------------------------------------//