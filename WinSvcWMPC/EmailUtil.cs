using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;

namespace WinSvcWMPC
{
    public class EmailUtil
    {
        public void EnviarViaSMTP(String Assunto, String Corpo, String emailenviar, int porta, String senha, String servidor, List<String> Emails, List<String> cc, List<String> cco, List<String> Anexos, Boolean ssl)
        {
            try
            {
                Dictionary<string, string> parametros = new Dictionary<string, string>();
                String host = String.IsNullOrEmpty(servidor) ? getHost(emailenviar) : servidor;
                SmtpClient SmtpServer = new SmtpClient(host, porta);
                SmtpServer.UseDefaultCredentials = false;
                SmtpServer.EnableSsl = ssl;
                SmtpServer.Credentials = new System.Net.NetworkCredential(emailenviar, senha);
                SmtpServer.DeliveryMethod = SmtpDeliveryMethod.Network;

                Emails = Emails ?? new List<String>();
                cc = cc ?? new List<String>();
                cco = cco ?? new List<String>();
                Anexos = Anexos ?? new List<String>();
                MailMessage mail = new MailMessage();
                mail.IsBodyHtml = true;
               
                mail.Body = Corpo;
                mail.Subject = Assunto;
                mail.From = new MailAddress(Emails[0]);

                Emails.ForEach(o => mail.To.Add(o));
                cc.ForEach(o => mail.CC.Add(o));
                cco.ForEach(o => mail.Bcc.Add(o));

                SmtpServer.Send(mail);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private string getHost(string emailenviar)
        {
            string[] array = emailenviar.Split('@');

            if (array != null && array.Count() > 0)
            {
                switch (array[1].ToUpper())
                {
                    case "GMAIL.COM":
                        return "smtp.googlemail.com";
                    default:
                        return string.Empty;
                }
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
