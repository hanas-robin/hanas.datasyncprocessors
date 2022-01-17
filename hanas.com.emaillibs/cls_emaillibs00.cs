using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mail;


namespace hanas.com.emaillibs
{
    public class cls_emaillibs00
    {
        private static EmailServerInformation _EmailServerInfo;
        private static EmailDocsInformation _EmailDocsInfo;


        public cls_emaillibs00()
        {
            SettingServerVariables();
            SettingDocsVariables();
        }


        //public EmailServerInformation EmailServerInfo
        //{
        //    set { _EmailServerInfo = value; }
        //}

        //public EmailDocsInformation EmailDocsInfo
        //{
        //    set { _EmailDocsInfo = value; }
        //}

        public string EmailReceipient
        {
            set { _EmailDocsInfo.em_receipient = value; }
        }

        public string EmailReceipientName
        {
            set { _EmailDocsInfo.em_receipientname = value; }
        }

        public string EmailSubject
        {
            set { _EmailDocsInfo.em_subject = value; }
        }

        public string EmailSender
        {
            set { _EmailDocsInfo.em_sender = value; }
        }

        public string EmailSenderName
        {
            set { _EmailDocsInfo.em_sendername = value; }
        }

        public string EmailBody
        {
            set { _EmailDocsInfo.em_body = value; }
        }
        
        public void SettingServerVariables()
        {
            _EmailServerInfo.ems_server = "mail.hanasolution.com";
            _EmailServerInfo.ems_port = 25;
            _EmailServerInfo.ems_ssl = false;
            _EmailServerInfo.ems_account = "support@hanasolution.com";
            _EmailServerInfo.ems_password = "v4pT_yX(AFd4";
            //_EmailServerInfo.ems_server = "smtp.gmail.com";
            //_EmailServerInfo.ems_port = 465;
            //_EmailServerInfo.ems_ssl = true;
            //_EmailServerInfo.ems_timeout = 60;
            //_EmailServerInfo.ems_account = "hanas.remote@gmail.com";
            //_EmailServerInfo.ems_password = "GKSKGKSK";
        }

        public void SettingDocsVariables()
        {
            _EmailDocsInfo.em_receipient = "alert@hanasolution.com";
            _EmailDocsInfo.em_receipientname = "Hanas Alert";
            _EmailDocsInfo.em_subject = "Hanas Auto Processing Alert - Do Not Reply";
            _EmailDocsInfo.em_sender = "alert@hanasolution.com";
            _EmailDocsInfo.em_sendername = "Hanas Auto Processor";
            _EmailDocsInfo.em_body = "System Processor Mailing";
        }

        public void SendEmail()
        {
            string sReceipient = string.Empty;
            string sSubject = string.Empty;

            MailMessage mEmail = new MailMessage();

            //if (_EmailDocsInfo.em_receipient.CompareTo("") == 0)
            //{
                //SettingDocsVariables();
            //}

            sReceipient = _EmailDocsInfo.em_receipientname + " <" + _EmailDocsInfo.em_receipient + ">";
            mEmail.Subject = _EmailDocsInfo.em_subject;

            mEmail.From = new MailAddress(_EmailDocsInfo.em_sender, _EmailDocsInfo.em_sendername);
            mEmail.To.Add(sReceipient);
            mEmail.Bcc.Add("Robin Hahm <robin.hahm@hanasolution.com>");
            mEmail.Subject = _EmailDocsInfo.em_subject;
            mEmail.Body = "<html><body><div>" +
                          _EmailDocsInfo.em_body +
                          "</div></body></html>";

            mEmail.IsBodyHtml = true;

            //if (_EmailServerInfo.ems_server.CompareTo("") == 0)
            //{
                //SettingServerVariables();
            //}

            SmtpClient SmtpServer = new SmtpClient(_EmailServerInfo.ems_server);

            SmtpServer.Port = _EmailServerInfo.ems_port;
            SmtpServer.Credentials = new System.Net.NetworkCredential(_EmailServerInfo.ems_account, _EmailServerInfo.ems_password);
            SmtpServer.EnableSsl = _EmailServerInfo.ems_ssl;
            SmtpServer.DeliveryMethod = SmtpDeliveryMethod.Network;
            SmtpServer.Timeout = _EmailServerInfo.ems_timeout;

            try
            {
                SmtpServer.Send(mEmail);
            }
            catch (Exception)
            {
               
                //c_sMessage = ex.Message.ToString();
                //c_colib.cWriteLogs(sProcessor, c_sMessage);
            }
        }

        public void AlertSendEmail()
        {
            string sReceipient = string.Empty;

            MailMessage mEmail = new MailMessage();

            if (_EmailServerInfo.ems_server.CompareTo("") == 0)
            {
                SettingServerVariables();
            }

            SmtpClient SmtpServer = new SmtpClient(_EmailServerInfo.ems_server);

            SmtpServer.Port = _EmailServerInfo.ems_port;
            SmtpServer.Credentials = new System.Net.NetworkCredential(_EmailServerInfo.ems_account, _EmailServerInfo.ems_password);
            SmtpServer.EnableSsl = _EmailServerInfo.ems_ssl;
            SmtpServer.DeliveryMethod = SmtpDeliveryMethod.Network;

            if (_EmailDocsInfo.em_receipient.CompareTo("") == 0)
            {
                SettingDocsVariables();
            }

            sReceipient = _EmailDocsInfo.em_receipientname + " <" + _EmailDocsInfo.em_receipient + ">";

            mEmail.From = new MailAddress(_EmailDocsInfo.em_sender, _EmailDocsInfo.em_sendername);
            mEmail.To.Add(sReceipient);
            mEmail.Bcc.Add("Robin Hahm <robin.hahm@hanasolution.com>");
            mEmail.Subject = _EmailDocsInfo.em_subject;
            mEmail.Body = "<html><body><div>" +
                          _EmailDocsInfo.em_body +
                          "</div></body></html>";

            mEmail.IsBodyHtml = true;

            try
            {
                SmtpServer.Send(mEmail);
            }
            catch (Exception)
            {
                // sMessage = ex.Message.ToString();
            }
        }
    }
}
