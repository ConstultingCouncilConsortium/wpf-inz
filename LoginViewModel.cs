using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace wpf_inz
{
    public class LoginViewModel
    {
        public string UserIconPath { get; }
        public string LockIconPath { get; }

        public LoginViewModel()
        {
            string projectPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName);
            UserIconPath = Path.Combine(projectPath, "Resources", "user_icon.svg");
            LockIconPath = Path.Combine(projectPath, "Resources", "lock_icon.svg");
        }

        public int VerifyUser(string email, string password)
        {
            string hashedPassword = HashPassword(password);
            using (var context = new ApplicationDbContext())
            {
                var user = context.Users.SingleOrDefault(u => u.Email == email && u.PasswordHash == hashedPassword);

                if (user != null)
                {
                    if (!user.IsEmailVerified)
                    {
                        return 2; // Poprawne dane logowania brak weryfikacji
                    }
                    return 1; // Poprawne dane logowania i zweryfikowany użytkownik
                }
            }
            return 0; // Niepoprawne dane logowania
        }

        public static string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        public void GenerateNotificationsForUser(int userId)
        {
            using (var context = new ApplicationDbContext())
            {
                var now = DateTime.Now;
                var tomorrow = now.Date.AddDays(1);
                var dayAfterTomorrow = tomorrow.AddDays(1);

                var unifiedEvents = context.UnifiedEvents
                    .Where(ue =>
                        (ue.EventType == "GeneralNote" && context.GeneralNotes.Any(gn => gn.Id == ue.ReferenceId && gn.UserId == userId && gn.Date >= tomorrow && gn.Date < dayAfterTomorrow)) ||
                        (ue.EventType == "ConfirmedAppointment" && context.ConfirmedAppointments.Any(ca => ca.Id == ue.ReferenceId && ca.UserId == userId && ca.Date >= tomorrow && ca.Date < dayAfterTomorrow)) ||
                        (ue.EventType == "WasteSchedule" && context.WasteSchedules.Any(ws => ws.Id == ue.ReferenceId && ws.UserId == userId && ws.Date >= tomorrow && ws.Date < dayAfterTomorrow)))
                    .ToList();

                foreach (var unifiedEvent in unifiedEvents)
                {
                    if (context.Notifications.Any(n => n.UnifiedEventId == unifiedEvent.Id)) continue;

                    string message = unifiedEvent.EventType switch
                    {
                        "GeneralNote" => $"Masz notatkę z jutrzejszą datą.",
                        "ConfirmedAppointment" => $"Masz potwierdzone spotkanie zaplanowane na jutro.",
                        "WasteSchedule" => $"Masz zaplanowany wywóz śmieci na jutro.",
                        _ => $"Masz nowe wydarzenie na jutro."
                    };

                    context.Notifications.Add(new Notification
                    {
                        UnifiedEventId = unifiedEvent.Id,
                        UserId = userId,
                        Message = message,
                        IsRead = false,
                        CreatedAt = now
                    });
                }

                context.SaveChanges();
            }
        }





    }
}
