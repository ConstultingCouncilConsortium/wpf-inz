using System;
using System.Net;
using System.Net.Mail;

public class EmailService
{
    private string _smtpServer = "smtp.gmail.com";
    private int _smtpPort = 587;
    private string _fromEmail = "alekdom264@gmail.com"; // Twój adres Gmail
    private string _fromPassword = "lgks qckz ysdx ugax"; // Wygenerowane hasło aplikacji

    public void SendVerificationEmail(string toEmail, string verificationCode)
    {
        try
        {
            // Treść wiadomości e-mail z kodem weryfikacyjnym
            string subject = "Potwierdzenie adresu e-mail";
            string body = $"Twój kod weryfikacyjny to: {verificationCode}\n" +
                          "Wpisz ten kod w aplikacji, aby potwierdzić swój adres e-mail.";

            // Konfiguracja klienta SMTP
            var smtpClient = new SmtpClient(_smtpServer, _smtpPort)
            {
                Credentials = new NetworkCredential(_fromEmail, _fromPassword),
                EnableSsl = true
            };

            // Tworzenie wiadomości e-mail
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail),
                Subject = subject,
                Body = body
            };
            mailMessage.To.Add(toEmail);

            // Wysyłanie wiadomości
            smtpClient.Send(mailMessage);
            Console.WriteLine("Wiadomość e-mail z kodem weryfikacyjnym została wysłana.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd przy wysyłaniu e-maila: {ex.Message}");
        }
    }
}
