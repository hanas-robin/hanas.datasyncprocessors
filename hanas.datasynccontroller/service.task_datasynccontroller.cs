using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Threading;
using System.Timers;
using System.ComponentModel;
using System.Diagnostics;
using System.Data;
using System.Configuration;

using hanas.com.covar;
using hanas.com.codb;
using hanas.com.colibs;
using hanas.com.isecure;
using hanas.com.emaillibs;

namespace hanas.datasynccontroller
{
    public partial class task_datasynccontroller : ServiceBase
    {
        private cls_codb00 c_localdb = new cls_codb00();
        private cls_codb00 c_remotedb = new cls_codb00();

        private cls_covar00 c_covar = new cls_covar00();
        private cls_colibs00 c_colib = new cls_colibs00();
        private cls_isecure00 _isSecure = new cls_isecure00();
        private cls_emaillibs00 c_emaillib = new cls_emaillibs00();

        private ADODB.Connection c_local_dbcon;
        private ADODB.Connection c_remote_dbcon;

        private ADODB.Recordset c_local_rs;
        private ADODB.Recordset c_remote_rs;

        private System.Timers.Timer _tmrLapse = new System.Timers.Timer();

        private string c_sMessage = string.Empty;
        private string c_config_file = "hanas.settings";

        private string c_sProcessor = System.AppDomain.CurrentDomain.FriendlyName.Replace(".exe", "");
        private string c_sMethod;

        private string c_sStartDate;    // = DateTime.Now.ToString("yyyy-MM-dd");
        private string c_sStartTime;    // = "07:00:00";
        private string c_sEndDate;      // = "2999-12-31";
        private string c_sEndTime;      // = "19:00:00";
        private int c_iTimerInterval;   // = 1;         // Default 1 sec
        private int c_iProcInterval;    // = 1;         // Default 1 minute

        private bool c_bTimeOn = false, c_bStartOn = true;

        private Boolean c_bFinished = false;
        private Boolean c_bLocalVerified = false;
        private Boolean c_bRemoteConnection = false;

        private string c_outprocessor_args = string.Empty;
        private string c_inprocessor_args = string.Empty;

        private static Object obj = new Object();

        private BackgroundWorker _bgwDataProcessors = new BackgroundWorker();
        private Process _InProcessor;
        private Process _OutProcessor;

        public task_datasynccontroller()
        {
            _tmrLapse.Elapsed += new ElapsedEventHandler(_tmrLapse_Elapsed);

            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            c_sMessage = "OnStart Process was started (" + c_sMethod + ")";
            c_colib.cWriteLogs(c_sProcessor, c_sMessage);

            SetSystemDBConnectionInfo();
            SetDefaultTimerInfo();

            PreProcessingJobs();

            GetTimerInfo();
            //StartTimerOn();
        }

        protected override void OnStop()
        {
            c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            c_sMessage = "Process has been stoppted (" + c_sMethod + ")";
            c_colib.cWriteLogs(c_sProcessor, c_sMessage);
        }

        private void PreProcessingJobs()
        {
            c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            c_colib.cWriteLogs(c_sProcessor, "DataSyncController Started (" + c_covar.host_localcode + ") (" + c_sMethod + ")!!");

            try
            {
                VerifyHostParameters();

                if (c_bLocalVerified)
                {
                    //c_bRemoteConnection = false;
                    //ThreadPool.QueueUserWorkItem(ChkRemoteHostConnection);

                    //Thread.Sleep(5000);

                    c_emaillib.EmailSubject = "[" + c_covar.host_localname + "] Batch Controller Started";
                    c_emaillib.EmailBody = c_sMethod + " Local Host was VERIFIED.";
                    c_emaillib.SendEmail();
                }
                else
                {
                    c_colib.cWriteLogs(c_sProcessor, "Local Server is NOT VERIFIED (" + c_sMethod + ")!!");
                    //Thread.Sleep(1000);

                    c_emaillib.EmailSubject = "[" + c_covar.host_localname + "] Batch Controller NOT Started";
                    c_emaillib.EmailBody = c_sMethod + " Local Host was NOT VERIFIED.";
                    c_emaillib.SendEmail();

                    return;
                }

                InitializeBackgroundWorker();
            }
            catch (DataException ex)
            {
                c_colib.cWriteLogs(c_sProcessor, "Database Connection Error: {" + ex.Message + "}, Please contact the system administrator (" + c_sMethod + ")!!");

                c_emaillib.EmailSubject = "[" + c_covar.host_localname + "] Batch Controller Stopped";
                c_emaillib.EmailBody = "Database Connection Error: {" + ex.Message + "} (" + c_sMethod + ").";
                c_emaillib.SendEmail();
            }
            finally
            {
                if (c_localdb != null) c_localdb.DBClose();
                if (c_remotedb != null) c_remotedb.DBClose();
            }
        }

        private void SetDefaultTimerInfo()
        {
            c_sStartDate = DateTime.Now.ToString("yyyy-MM-dd");
            c_sStartTime = "07:00:00";
            c_sEndDate = "2999-12-31";
            c_sEndTime = "19:00:00";
            c_iTimerInterval = 1000;        // Default 1 sec
            c_iProcInterval = 60000;        // Default 1 min
        }

        private void StartTimerOn()
        {
            c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            _tmrLapse.Interval = c_iTimerInterval; //number in milisecinds, 1000: 1 sec
            _tmrLapse.AutoReset = true;
            _tmrLapse.Enabled = true;
            _tmrLapse.Start();

            c_colib.cWriteLogs(c_sProcessor, "Timer Started (" + c_sMethod + ")!!");
        }

        private void _tmrLapse_Elapsed(object source, EventArgs e)
        {
            c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            //c_colib.cWriteLogs(c_sProcessor, "Timer elapsed - Call RunWorkerAsync (" + c_sMethod + ")!!");

            try
            {
                if (c_sStartDate.CompareTo(DateTime.Now.ToString("yyyy-MM-dd")) <= 0 && c_sEndDate.CompareTo(DateTime.Now.ToString("yyyy-MM-dd")) >= 0)
                {
                    if ((!c_bTimeOn && c_sStartTime.CompareTo(DateTime.Now.ToString("HH:mm:ss")) == 0) || (c_bStartOn && c_sStartTime.CompareTo(DateTime.Now.ToString("HH:mm:ss")) < 0))
                    {
                        c_colib.cWriteLogs(c_sProcessor, "Scheduler On (" + c_sMethod + ")!!");

                        c_bTimeOn = true;
                        c_bStartOn = false;                 // 첫 실행 여부 판단
                        c_bFinished = false;

                        this._bgwDataProcessors.RunWorkerAsync(1);                       // _bgwDataProcessors_DoWork 프로세스 트리거

                        _tmrLapse.Stop();
                        _tmrLapse.Close();

                        _tmrLapse.Start();
                    }

                    if (c_bTimeOn && c_sEndTime.CompareTo(DateTime.Now.ToString("HH:mm:ss")) < 0)
                    {
                        c_colib.cWriteLogs(c_sProcessor, "Set finished (" + c_sMethod + ")!!");

                        c_bTimeOn = false;
                        c_bFinished = true;
                    }
                }
                else
                {
                    c_colib.cWriteLogs(c_sProcessor, "Please check available date (" + c_sMethod + ")!!");
                }
            }
            catch
            {
                //c_sMessage = sBodyMessage + "<br />" + c_sMessage;
            }
            finally
            {
                if (c_bTimeOn)
                {
                    //c_emaillib.EmailSubject = "[" + c_covar.host_localname + "] Auto Processor alert";
                    //c_emaillib.EmailBody = sBodyMessage;
                    //c_emaillib.SendEmail();

                    //c_colib.cWriteLogs(c_sProcessor, "[" + sProcedure + "] Process done!!");
                }
            }
        }

        private void InitializeBackgroundWorker()
        {
            this._bgwDataProcessors.DoWork += new DoWorkEventHandler(_bgwDataProcessors_DoWork);
            this._bgwDataProcessors.RunWorkerCompleted += new RunWorkerCompletedEventHandler(_bgwDataProcessors_RunWorkerCompleted);
            this._bgwDataProcessors.ProgressChanged += new ProgressChangedEventHandler(_bgwDataProcessors_ProgressChanged);
        }

        private void _bgwDataProcessors_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            c_colib.cWriteLogs(c_sProcessor, "DoWork triggered (" + c_sMethod + ")!!");
            // Assign the result of the computation to the Result property of the DoWorkEventArgs object. This is will be available to the RunWorkerCompleted eventhandler.
            e.Result = RunDataSyncProcessors((int)e.Argument, worker, e);
        }

        private long RunDataSyncProcessors(int n, BackgroundWorker worker, DoWorkEventArgs e)
        {
            bool bStopped = false;

            c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            c_colib.cWriteLogs(c_sProcessor, "Backgoundwork started.. (" + c_sMethod + ")!!");

            if (worker.CancellationPending)
            {
                e.Cancel = true;

                c_colib.cWriteLogs(c_sProcessor, "Work cancelled (" + c_sMethod + ")!!");
            }
            else
            {
                _tmrLapse.Stop();
                _tmrLapse.Close();

                ProcessStartInfo psiInProcessor = new ProcessStartInfo("hanas.datasyncin.exe", c_inprocessor_args);
                psiInProcessor.UseShellExecute = false;

                _InProcessor = null;            // new Process();
                _InProcessor = Process.Start(psiInProcessor);
                //c_colib.cWriteLogs(c_sProcessor, "_InProcessor started : " + c_inprocessor_args + " (" + c_sMethod + ")!!");

                ProcessStartInfo psiOutProcessor = new ProcessStartInfo("hanas.datasyncout.exe", c_outprocessor_args);
                psiOutProcessor.UseShellExecute = false;

                _OutProcessor = null;           // new Process();
                _OutProcessor = Process.Start(psiOutProcessor);
                //c_colib.cWriteLogs(c_sProcessor, "_OutProcessor started : " + c_outprocessor_args + " (" + c_sMethod + ")!!");

                while (true)                     // Main Process will be finished at a certain time from scheduler
                {
                    if (c_bFinished)
                    {
                        //c_colib.cWriteLogs(c_sProcessor, "Jobs finished (" + System.Reflection.MethodBase.GetCurrentMethod().Name + bStopped.ToString() + ")!!");
                        if (!bStopped)
                        {
                            bStopped = true;
                            c_colib.cWriteLogs(c_sProcessor, "Jobs finished (" + c_sMethod + ")!!");
                            Thread.Sleep(5000);
                            GetTimerInfo();
                        }
                    }
                    else
                    { 
                        if (bStopped)
                        {
                            //c_colib.cWriteLogs(c_sProcessor, "Processors started (" + c_sMethod + ")!!");
                            bStopped = false;
                        }

                        if (_InProcessor != null && !_InProcessor.HasExited)
                        {
                            c_colib.cWriteLogs(c_sProcessor, "_InProcessor alive (" + c_sMethod + ")!!");
                        }
                        else
                        {
                            _InProcessor = Process.Start(psiInProcessor);
                            //_InProcessor = Process.Start("hanas.datasyncin.exe", c_inprocessor_args);
                            //c_colib.cWriteLogs(c_sProcessor, "_InProcessor started (" + c_sMethod + ")!!");

                            //i++;
                        }

                        //Thread.Sleep(3000);

                        if (_OutProcessor != null && !_OutProcessor.HasExited)
                        {
                            c_colib.cWriteLogs(c_sProcessor, "_OutProcessor alive (" + c_sMethod + ")!!");
                        }
                        else
                        {
                            //if (c_bRemoteConnection)
                            //{
                            _OutProcessor = Process.Start(psiOutProcessor);
                            //c_colib.cWriteLogs(c_sProcessor, "_OutProcessor started (" + c_sMethod + ")!!");

                            //i++;
                            //}
                        }

                    }

                    Thread.Sleep(c_iProcInterval);
                }
            }

            c_colib.cWriteLogs(c_sProcessor, "Exiting.. (" + c_sMethod + ")!!");

            return 0;
        }

/*********************************************
        private void KeepProcessorOn()
        {
            Boolean bFailed = true;

            c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            if (c_covar.host_remotename == "")
            {
                c_bRemoteConnection = false;
                c_colib.cWriteLogs(c_sProcessor, "Remote Host NOT VERIFIED (" + c_sMethod + ")!!");
            }
            else
            {
                c_colib.cWriteLogs(c_sProcessor, "Remote Host VERIFIED (" + c_sMethod + ")!!");
                c_colib.cWriteLogs(c_sProcessor, "Start Remote Database Connection (" + c_sMethod + ")!!");

                c_bRemoteConnection = false;
                ThreadPool.QueueUserWorkItem(ChkRemoteHostConnection);
            }

            while (true)
            {
                if (c_bLocalVerified)
                {

                }
                else
                {

                }

                if (c_bRemoteConnection)
                {
                    if (bFailed)
                    {
                        c_colib.cWriteLogs(c_sProcessor, "Remote Database is Connected (" + c_sMethod + ")!!");
                        bFailed = false;
                    }
                }
                else
                {
                    bFailed = true;
                    c_colib.cWriteLogs(c_sProcessor, "Remote Database is NOT Connected (" + c_sMethod + ")!!");
                }

                ThreadPool.QueueUserWorkItem(ChkRemoteHostConnection);

                //if (c_finished) break;

                Thread.Sleep(60000);

                //Application.DoEvents();
            }

            //c_localdb.CloseSymmetricKey();
        }
***************************************************/

        private void SetSystemDBConnectionInfo()
        {
            Configuration cfConfigManager = null;
            string sConfigPath = this.GetType().Assembly.Location;

            cfConfigManager = ConfigurationManager.OpenExeConfiguration(sConfigPath);

            c_localdb.db_secure_key = String.IsNullOrEmpty(_isSecure.GetAppSetting(cfConfigManager, "DBSecurityKey")) ? "" : _isSecure.tDecrypt(_isSecure.GetAppSetting(cfConfigManager, "DBSecurityKey"));
            c_localdb.db_host = String.IsNullOrEmpty(_isSecure.GetAppSetting(cfConfigManager, "sysdbhost")) ? "" : _isSecure.tDecrypt(_isSecure.GetAppSetting(cfConfigManager, "sysdbhost"));   //10.0.1.1
            c_localdb.db_port = String.IsNullOrEmpty(_isSecure.GetAppSetting(cfConfigManager, "sysdbport")) ? "" : _isSecure.tDecrypt(_isSecure.GetAppSetting(cfConfigManager, "sysdbport"));
            c_localdb.db_name = String.IsNullOrEmpty(_isSecure.GetAppSetting(cfConfigManager, "sysdbname")) ? "" : _isSecure.tDecrypt(_isSecure.GetAppSetting(cfConfigManager, "sysdbname"));
            c_localdb.db_userid = String.IsNullOrEmpty(_isSecure.GetAppSetting(cfConfigManager, "sysdbuserid")) ? "" : _isSecure.tDecrypt(_isSecure.GetAppSetting(cfConfigManager, "sysdbuserid"));
            c_localdb.db_password = String.IsNullOrEmpty(_isSecure.GetAppSetting(cfConfigManager, "sysdbpassword")) ? "" : _isSecure.tDecrypt(_isSecure.GetAppSetting(cfConfigManager, "sysdbpassword"));
        }

        private void VerifyHostParameters()
        {
            string sQBuff = string.Empty;

            c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            if (c_localdb.DBConnection() < 1)
            {
                c_bLocalVerified = false;
                c_colib.cWriteLogs(c_sProcessor, "Local Database Connection Failed (" + c_sMethod + ") - " + c_localdb.db_host);

                return;
            }

            c_localdb.OpenSymmetricKey();

            //Getting Local Server Information
            sQBuff = "SELECT bi_ename, bi_hostname, " +
                             "CAST(DecryptByKey(bi_dbhost) AS varchar(100)) AS dbhost, " +
                             "CAST(DecryptByKey(bi_dbport) AS varchar(100)) AS dbport, " +
                             "CAST(DecryptByKey(bi_dbuserid) AS varchar(100)) AS dbuserid, " +
                             "CAST(DecryptByKey(bi_dbpassword) AS varchar(100)) AS dbpassword " +
                        "FROM tb_branchinfo " +
                       "WHERE bi_hosttype = '" + c_covar.host_type + "' " +
                         "AND bi_cd = '" + c_covar.host_localcode + "' " +
                         "AND bi_active = '1'";

            //c_colib.cWriteLogs(c_sProcessor, "sQBuff: " + sQBuff + " (" + c_sMethod + ")!!");

            if (c_localdb.RsOpen(sQBuff) > 0 && c_localdb.rs.RecordCount > 0)
            {
                if (Convert.IsDBNull(c_localdb.rs.Fields["bi_hostname"].Value))
                {
                    c_bLocalVerified = false;
                    c_colib.cWriteLogs(c_sProcessor, "Local Hostname is Not identified (" + c_sMethod + ")!!");
                }
                else
                {
                    //Arguments와 Database 등록 정보가 일치한지 검증
                    if (Convert.ToString(c_localdb.rs.Fields["bi_hostname"].Value) == c_covar.host_localname.ToUpper())
                    {
                        c_bLocalVerified = true;

                        c_inprocessor_args = @"-t" + c_covar.host_type + " -l" + c_covar.host_localname + " -c" + c_covar.host_localcode;

                        string sMessage = string.Format("Local Hostname is Verified: {0}, Local Name: {1} / {6}, Local Code: {2}, Remote Name: {3}, Remote Code: {4} [{5}]", c_covar.host_type, c_covar.host_localname, c_covar.host_localcode, c_covar.host_remotename, c_covar.host_remotecode, c_sMethod, Convert.ToString(c_localdb.rs.Fields["bi_hostname"].Value));
                        //c_colib.cWriteLogs(c_sProcessor, sMessage);

                        //c_colib.cWriteLogs(c_sProcessor, "Local Hostname is Verified (" + c_sMethod + ")!!");

                        c_localdb.RsClose();

                        sQBuff = "SELECT bi_ename, bi_hostname, " +
                                        "CAST(DecryptByKey(bi_dbhost) AS varchar(100)) AS dbhost, " +
                                        "CAST(DecryptByKey(bi_dbport) AS varchar(100)) AS dbport, " +
                                        "CAST(DecryptByKey(bi_dbuserid) AS varchar(100)) AS dbuserid, " +
                                        "CAST(DecryptByKey(bi_dbpassword) AS varchar(100)) AS dbpassword " +
                                   "FROM tb_branchinfo " +
                                  "WHERE bi_cd = '" + c_covar.host_remotecode + "' " +
                                    "AND bi_hosttype = 'S' " +
                                    "AND bi_hostname = '" + c_covar.host_remotename + "' " +
                                    "AND bi_active = '1'";

                        if (c_localdb.RsOpen(sQBuff) > 0 && c_localdb.rs.RecordCount > 0)
                        {
                            c_remotedb.db_host = Convert.ToString(c_localdb.rs.Fields["dbhost"].Value);
                            c_remotedb.db_port = Convert.ToString(c_localdb.rs.Fields["dbport"].Value);
                            c_remotedb.db_userid = Convert.ToString(c_localdb.rs.Fields["dbuserid"].Value);
                            c_remotedb.db_password = Convert.ToString(c_localdb.rs.Fields["dbpassword"].Value);

                            c_outprocessor_args = @"-t" + c_covar.host_type + " -l" + c_covar.host_localname + " -c" + c_covar.host_localcode + " -r" + c_covar.host_remotename + " -s" + c_covar.host_remotecode;
                        }
                        else
                        {
                            c_colib.cWriteLogs(c_sProcessor, "Retrieving Remote Database Info FAILED (" + c_sMethod + ")!!");
                            c_bRemoteConnection = false;
                            c_bLocalVerified = false;
                        }
                    }
                    else
                    {
                        c_bLocalVerified = false;
                        //c_colib.cWriteLogs(c_sProcessor, "Local Hostname is Not Verified (" + c_sMethod + ")!!");

                        string sMessage = string.Format("Local Hostname is Verified: {0}, Local Name: {1} / {6}, Local Code: {2}, Remote Name: {3}, Remote Code: {4} [{5}]", c_covar.host_type, c_covar.host_localname, c_covar.host_localcode, c_covar.host_remotename, c_covar.host_remotecode, c_sMethod, Convert.ToString(c_localdb.rs.Fields["bi_hostname"].Value));
                        //c_colib.cWriteLogs(c_sProcessor, sMessage);
                    }
                }
            }
            else
            {
                c_bLocalVerified = false;
                c_colib.cWriteLogs(c_sProcessor, "Local Hostname does NOT Exist (" + c_sMethod + ")!!");
                //c_colib.cWriteLogs(c_sProcessor, "Local Database Connection Failed (" + sQBuff + ")!!");
                //c_colib.cWriteLogs(c_sProcessor, "Local Database Connection Failed (" + c_localdb.db_params + ")!!");
            }

            c_localdb.RsClose();
            c_localdb.CloseSymmetricKey();
            c_localdb.DBClose();
        }

        private void ChkRemoteHostConnection(Object state)
        {
            //string s_qbuff = string.Empty;

            //if (c_localdb.DBConnection() < 1)
            //{
            //    c_colib.cWriteLogs(c_sProcessor, "Local Database Connection FAILED (" + c_sMethod + ")!!");
            //    return;
            //}

            //c_localdb.OpenSymmetricKey();
            ////Getting Remote Server Information
            //s_qbuff = "SELECT bi_ename, bi_hostname, " +
            //                 "CAST(DecryptByKey(bi_dbhost) AS varchar(100)) AS dbhost, " +
            //                 "CAST(DecryptByKey(bi_dbport) AS varchar(100)) AS dbport, " +
            //                 "CAST(DecryptByKey(bi_dbuserid) AS varchar(100)) AS dbuserid, " +
            //                 "CAST(DecryptByKey(bi_dbpassword) AS varchar(100)) AS dbpassword " +
            //            "FROM tb_branchinfo " +
            //           "WHERE bi_cd = '" + c_covar.host_remotecode + "' " +
            //             "AND bi_hosttype = 'S' " +
            //             "AND bi_hostname = '" + c_covar.host_remotename + "' " +
            //             "AND bi_active = '1'";

            //if (c_localdb.RsOpen(s_qbuff) > 0 && c_localdb.rs.RecordCount > 0)
            //{
            //    //c_remotedb = new cls_codb00();

            //    c_remotedb.db_host = Convert.ToString(c_localdb.rs.Fields["dbhost"].Value);
            //    c_remotedb.db_port = Convert.ToString(c_localdb.rs.Fields["dbport"].Value);
            //    c_remotedb.db_userid = Convert.ToString(c_localdb.rs.Fields["dbuserid"].Value);
            //    c_remotedb.db_password = Convert.ToString(c_localdb.rs.Fields["dbpassword"].Value);

            //    c_outprocessor_args = @"-t" + c_covar.host_type + " -l" + c_covar.host_localname + " -c" + c_covar.host_localcode + " -r" + c_covar.host_remotename + " -s" + c_covar.host_remotecode;

            c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            lock (obj)
            {
                if (c_remotedb.DBConnection() < 0)
                {
                    c_bRemoteConnection = false;
                    c_colib.cWriteLogs(c_sProcessor, "Remote Database Connection FAILED (" + c_sMethod + ")!! - " + c_outprocessor_args);
                }
                else
                {
                    c_bRemoteConnection = true;
                }

                c_remotedb.DBClose();
            }
            //}
            //else
            //{
            //    c_colib.cWriteLogs(c_sProcessor, "Retrieving Remote Database Info FAILED (" + c_sMethod + ")!!");
            //    c_bRemoteConnection = false;
            //    //lblRemote.BackColor = Color.Red;
            //}

            //c_localdb.CloseSymmetricKey();
            //c_localdb.RsClose();
            //c_localdb.DBClose();
        }

        private void GetTimerInfo()
        {
            int iListCount = 0, iReturn = 0;
            string[] sConfigs;
            List<string> lLine = new List<string>();

            c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            iReturn = c_colib.cReadFile(c_config_file, ref lLine);

            if (iReturn != 1)
            {
                c_sMessage = "Config file Error (" + c_sMessage + ")";
                c_colib.cWriteLogs(c_sProcessor, c_sMessage);

                return;
            }

            iListCount = lLine.Count;

            if (iListCount > 0)
            {
                for (int j = 0; j < iListCount; j++)
                {
                    sConfigs = lLine[j].Split(',');

                    switch (sConfigs[0])
                    {
                        case "Error":
                            c_sMessage = sConfigs[1];
                            c_colib.cWriteLogs(c_sProcessor, c_sMessage);
                            return;

                        case "StartDate":
                            c_sStartDate = sConfigs[1];
                            break;

                        case "StartTime":
                            c_sStartTime = sConfigs[1];
                            break;

                        case "EndDate":
                            c_sEndDate = sConfigs[1];
                            break;

                        case "EndTime":
                            c_sEndTime = sConfigs[1];
                            break;

                        case "Interval":
                            c_iTimerInterval = Convert.ToInt32(sConfigs[1]);
                            break;

                        case "ProcInterval":
                            c_iProcInterval = Convert.ToInt32(sConfigs[1]);
                            break;

                        default:
                            break;
                    }
                }
            }

            c_sMessage = "Configuration file read completed (" + c_sMethod + ")";
            c_colib.cWriteLogs(c_sProcessor, c_sMessage);

            StartTimerOn();
        }

        private void StopProcess()
        {
            c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            Stop();
            Environment.Exit(0);

            c_sMessage = "Stopping (" + c_sMethod + ")";
            c_colib.cWriteLogs(c_sProcessor, c_sMessage);

            return;
        }

        private void _bgwDataProcessors_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            if (e.Error != null)
            {
                c_bTimeOn = false;
                c_bStartOn = true;

                c_colib.cWriteLogs(c_sProcessor, "Work completed with error (" + System.Reflection.MethodBase.GetCurrentMethod().Name + " - " + e.Error.ToString() + ")!!");
            }
            else if (e.Cancelled)
            {
                c_colib.cWriteLogs(c_sProcessor, "Work completed with cancel (" + System.Reflection.MethodBase.GetCurrentMethod().Name + " - " + e.Error.ToString() + ")!!");
            }
            else
            {
                // Finally, handle the case where the operation succeeded.
                c_colib.cWriteLogs(c_sProcessor, "Work completed with success (" + System.Reflection.MethodBase.GetCurrentMethod().Name + " - " + e.Error.ToString() + ")!!");
            }
        }

        // This event handler updates the progress bar.
        private void _bgwDataProcessors_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            //this.pgbMainProcessor.Value = e.ProgressPercentage;
            c_colib.cWriteLogs(c_sProcessor, "Processing changing.. (" + c_sMethod + " - " + "|||" + ")!!");
        }
    }
}
