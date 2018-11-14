using Microsoft.Exchange.WebServices.Data;
using System;
using System.Net;
using System.Security;

namespace QnABot
{
    public static class Mail
    {
        public static void SendMail()
        {
            ExchangeService service = new ExchangeService();
            var passWord = new SecureString();
            foreach (char c in "*******".ToCharArray())
            {
                passWord.AppendChar(c);
            }
            service.Credentials = new NetworkCredential("fbli@dow.com", passWord);
            service.Url = new Uri("https://outlook.office365.com/EWS/Exchange.asmx");

            EmailMessage emsg = new EmailMessage(service);
            emsg.Subject = "Your annual leave request has been created";
            emsg.From = "fbli@dow.com";
            emsg.ToRecipients.Add("fbli@dow.com");
            emsg.Body = "This message is sent by the web chat " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.AddHours(8).ToLongTimeString();
            emsg.Send();
        }
    }
}