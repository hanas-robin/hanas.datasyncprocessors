using System;
using System.Configuration;
using System.Net;
using System.Net.Sockets;

namespace hanas.com.covar
{
    public class cls_covar00
    {
        public AppSettingsReader _SettingsReader = new AppSettingsReader();

        private static string c_app_path = string.Empty;
        private static string c_image_path = string.Empty;
        private static string c_log_path = string.Empty;

        private static HostInformation c_HostInformation;


        public cls_covar00()
        {
            InitializingGlobalVariables();
        }


        public string host_type
        {
            set { c_HostInformation.host_type = value; }
            get { return c_HostInformation.host_type; }
        }

        public string host_localname
        {
            set { c_HostInformation.host_localname = value; }
            get { return c_HostInformation.host_localname; }
        }

        public string host_localipaddress
        {
            set { c_HostInformation.host_localipaddress = value; }
            get { return c_HostInformation.host_localipaddress; }
        }

        public string host_localcode
        {
            set { c_HostInformation.host_localcd = value; }
            get { return c_HostInformation.host_localcd; }
        }

        public string host_remotename
        {
            set { c_HostInformation.host_remotename = value; }
            get { return c_HostInformation.host_remotename; }
        }

        public string host_remoteipaddress
        {
            set { c_HostInformation.host_remoteipaddress = value; }
            get { return c_HostInformation.host_remoteipaddress; }
        }

        public string host_remotecode
        {
            set { c_HostInformation.host_remotecd = value; }
            get { return c_HostInformation.host_remotecd; }
        }


        public void InitializingGlobalVariables()
        {
            c_app_path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
            c_image_path = c_app_path + "\\images";
            c_log_path = c_app_path + "\\logs";

            c_HostInformation.host_localipaddress = GetLocalIPAddress();
            c_HostInformation.host_localname = c_HostInformation.host_localname.ToUpper() == "ROBINT15" ? "HANAS01" : c_HostInformation.host_localname;
        }

        public static string GetLocalIPAddress()
        {
            c_HostInformation.host_localname = Dns.GetHostName().ToUpper();

            var host = Dns.GetHostEntry(c_HostInformation.host_localname);
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
    }
}
