using System.Net.Mail;

namespace ElmahCore.WebUI.Notifiers
{
    public class EmailOptions
    {
        public string MailRecipient { get; set; }
        public string MailSender { get; set; }
        public string MailCopyRecipient { get; set; }
        public string MailSubjectFormat { get; set; }
        public MailPriority MailPriority { get; set; }
        public bool ReportAsynchronously { get; set; }
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string AuthUserName { get; set; }
        public string AuthPassword { get; set; }
        public bool SendYsod { get; set; }
        public bool UseSsl { get; set; }
    }
}