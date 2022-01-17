using System.ServiceProcess;

using hanas.com.covar;
using hanas.com.colibs;


namespace hanas.hpack
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        private static cls_colibs00 c_colib = new cls_colibs00();
        private static cls_covar00 c_covar = new cls_covar00();

        private static string c_sProcessor = System.AppDomain.CurrentDomain.FriendlyName.Replace(".exe", "");
        private static string c_sMethod;

        static void Main(string[] args)
        {
            int i = 0;

            //AppDomain.CurrentDomain.ProcessExit += Destructor;

            c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            if (args.Length == 5)
            {
                string flag = string.Empty;

                for (i = 0; i < args.Length; i++)
                {
                    flag = args[i].Substring(0, 2);

                    switch (flag)
                    {
                        case "-t":
                            c_covar.host_type = args[i].Substring(2, args[i].Length - 2).Trim();
                            break;

                        case "-l":
                            c_covar.host_localname = args[i].Substring(2, args[i].Length - 2).Trim();
                            break;

                        case "-c":
                            c_covar.host_localcode = args[i].Substring(2, args[i].Length - 2).Trim();
                            break;

                        case "-r":
                            c_covar.host_remotename = args[i].Substring(2, args[i].Length - 2).Trim();
                            break;

                        case "-s":
                            c_covar.host_remotecode = args[i].Substring(2, args[i].Length - 2).Trim();
                            break;

                        default:
                            break;
                    }
                }
            }
            else
            {
                c_colib.cWriteLogs(c_sProcessor, "Please check the parameters (" + c_sMethod + ")!!");
                c_colib.cWriteLogs(c_sProcessor, "Please check the parameters : " + c_covar.host_localname + ", " + args.Length.ToString() + " (" + c_sMethod + ")!!");

                //Thread.Sleep(3000);

                return;
            }

            //c_colib.cWriteLogs(c_sProcessor, "Please check the parameters : " + c_covar.host_localname + ", " + args.Length.ToString() + " (" + c_sMethod + ")!!");

            if (c_covar.host_type.Trim().CompareTo("") == 0 ||
                c_covar.host_localname.Trim().CompareTo("") == 0 ||
                c_covar.host_localcode.Trim().CompareTo("") == 0 ||
                c_covar.host_remotename.Trim().CompareTo("") == 0 ||
                c_covar.host_remotecode.Trim().CompareTo("") == 0)
            {
                c_colib.cWriteLogs(c_sProcessor, "Please Check Parameters (" + c_sMethod + ")!!");
                return;
            }

            string sMessage = string.Format("Host Type: {0}, Local Name: {1}, Local Code: {2}, Remote Name: {3}, Remote Code: {4} [{5}]", c_covar.host_type, c_covar.host_localname, c_covar.host_localcode, c_covar.host_remotename, c_covar.host_remotecode, c_sMethod);
            c_colib.cWriteLogs(c_sProcessor, sMessage);

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new task_datatransferproc()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
