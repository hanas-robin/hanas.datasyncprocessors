using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;


namespace hanas.com.colibs
{
    public class cls_colibs00
    {
        private string c_app_path = AppDomain.CurrentDomain.BaseDirectory;      // System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
        private static SemaphoreSlim _Semaphore = new SemaphoreSlim(1);

        //private static Semaphore _Semaphore = new Semaphore(0, 1);

        public void cWriteLogs(string Processor, string Message)
        {
            string sLogPath = c_app_path + "\\Logs\\";
            string sFileName = Processor + "_" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";

            string sFileFullName = sLogPath + sFileName;

            _Semaphore.Wait();

            try
            {
                if (!Directory.Exists(sLogPath))
                {
                    Directory.CreateDirectory(sLogPath);
                }

                if (!File.Exists(sFileFullName))
                {
                    // Create a file to write to.   
                    using (StreamWriter swLogFile = File.CreateText(sFileFullName))
                    {
                        swLogFile.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + Message);
                    }
                }
                else
                {
                    using (StreamWriter sw = File.AppendText(sFileFullName))
                    {
                        sw.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + Message);
                    }
                }
            }
            catch
            {

            }
            finally
            {
                _Semaphore.Release();
            }
        }

        public int cReadFile(string vFileName, ref List<string> vList)
        {
            string sReadLine = string.Empty;

            string sFilePath = c_app_path + "\\";
            string sFileName = vFileName;

            string sFileFullName = sFilePath + sFileName;
            int iReturn = 0;

            try
            {
                if (!File.Exists(sFileFullName))
                {
                    iReturn = -1;
                }
                else
                {
                    StreamReader sr = new StreamReader(sFileFullName);
                    while ((sReadLine = sr.ReadLine()) != null)
                    {
                        vList.Add(sReadLine);
                    }

                    sr.Close();

                    iReturn = 1;
                }

                return iReturn;
            }
            catch
            {
                return -1;
            }
        }
    }
}
