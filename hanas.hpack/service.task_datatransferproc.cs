using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Threading;
using System.Timers;
using System.ComponentModel;
using System.Diagnostics;
using System.Data;
using System.Configuration;
using ADODB;
using MySql.Data.MySqlClient;

using hanas.com.covar;
using hanas.com.codb;
using hanas.com.colibs;
using hanas.com.isecure;
using hanas.com.emaillibs;


namespace hanas.hpack
{
    public partial class task_datatransferproc : ServiceBase
    {
        private cls_codb00 c_localdb = new cls_codb00();
        private cls_codb01 c_remotedb = new cls_codb01();

        private cls_covar00 c_covar = new cls_covar00();
        private cls_colibs00 c_colib = new cls_colibs00();
        private cls_isecure00 _isSecure = new cls_isecure00();
        private cls_emaillibs00 c_emaillib = new cls_emaillibs00();

        private System.Timers.Timer _tmrLapse;              // = new System.Timers.Timer();
        private BackgroundWorker _bgwDataProcessors;            // = new BackgroundWorker();
        private static SemaphoreSlim _Semaphore;

        private DBConnectionInfo g_localdbinfo;
        private DBConnectionInfo g_remotedbinfo;

        private string g_sMessage = string.Empty;
        private string g_config_file = "hanas.hpack";

        private string g_sProcessor = System.AppDomain.CurrentDomain.FriendlyName.Replace(".exe", "");
        //private string c_sMethod;

        private string g_sStartDate;    // = DateTime.Now.ToString("yyyy-MM-dd");
        private string g_sStartTime;    // = "07:00:00";
        private string g_sEndDate;      // = "2999-12-31";
        private string g_sEndTime;      // = "19:00:00";
        private int g_iTimerInterval;   // = 1;         // Default 1 sec
        private int g_iProcInterval;    // = 1;         // Default 1 minute

        private bool g_bTimeOn = false, g_bStartOn = true, g_bStopped = true;

        private Boolean g_bFinished = false;
        private Boolean g_bLocalVerified = false;
        private Boolean g_bRemoteConnection = false;

        private static Object obj = new Object();


        public task_datatransferproc()
        {
            InitializeComponent();

            SetSystemDBConnectionInfo();
            SetDefaultTimerInfo();
            GetTimerInfo();

            PreProcessingJobs();
        }

        protected override void OnStart(string[] args)
        {
            string c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            g_sMessage = string.Format("[{0}] OnStart Process was started!!", c_sMethod);
            c_colib.cWriteLogs(g_sProcessor, g_sMessage);
        }

        private void SetSystemDBConnectionInfo()
        {
            Configuration cfConfigManager = null;
            string sConfigPath = this.GetType().Assembly.Location;

            cfConfigManager = ConfigurationManager.OpenExeConfiguration(sConfigPath);

            g_localdbinfo.db_secure_key = String.IsNullOrEmpty(_isSecure.GetAppSetting(cfConfigManager, "DBSecurityKey")) ? "" : _isSecure.tDecrypt(_isSecure.GetAppSetting(cfConfigManager, "DBSecurityKey"));
            g_localdbinfo.db_host = String.IsNullOrEmpty(_isSecure.GetAppSetting(cfConfigManager, "sysdbhost")) ? "" : _isSecure.tDecrypt(_isSecure.GetAppSetting(cfConfigManager, "sysdbhost"));   //10.0.1.1
            g_localdbinfo.db_port = String.IsNullOrEmpty(_isSecure.GetAppSetting(cfConfigManager, "sysdbport")) ? "" : _isSecure.tDecrypt(_isSecure.GetAppSetting(cfConfigManager, "sysdbport"));
            g_localdbinfo.db_name = String.IsNullOrEmpty(_isSecure.GetAppSetting(cfConfigManager, "sysdbname")) ? "" : _isSecure.tDecrypt(_isSecure.GetAppSetting(cfConfigManager, "sysdbname"));
            g_localdbinfo.db_userid = String.IsNullOrEmpty(_isSecure.GetAppSetting(cfConfigManager, "sysdbuserid")) ? "" : _isSecure.tDecrypt(_isSecure.GetAppSetting(cfConfigManager, "sysdbuserid"));
            g_localdbinfo.db_password = String.IsNullOrEmpty(_isSecure.GetAppSetting(cfConfigManager, "sysdbpassword")) ? "" : _isSecure.tDecrypt(_isSecure.GetAppSetting(cfConfigManager, "sysdbpassword"));
        }

        private void SetDefaultTimerInfo()
        {
            g_sStartDate = DateTime.Now.ToString("yyyy-MM-dd");
            g_sStartTime = "07:00:00";
            g_sEndDate = "2999-12-31";
            g_sEndTime = "19:00:00";
            g_iTimerInterval = 1000;        // Default 1 sec
            g_iProcInterval = 60000;        // Default 1 min
        }

        private void GetTimerInfo()
        {
            string c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            int iListCount = 0, iReturn = 0;
            string[] sConfigs;

            List<string> lLine = new List<string>();

            iReturn = c_colib.cReadFile(g_config_file, ref lLine);

            if (iReturn != 1)
            {
                g_sMessage = string.Format("[{0}] Config file Error!!", c_sMethod);
                c_colib.cWriteLogs(g_sProcessor, g_sMessage);

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
                            g_sMessage = sConfigs[1];
                            c_colib.cWriteLogs(g_sProcessor, g_sMessage);
                            return;

                        case "StartDate":
                            g_sStartDate = sConfigs[1];
                            break;

                        case "StartTime":
                            g_sStartTime = sConfigs[1];
                            break;

                        case "EndDate":
                            g_sEndDate = sConfigs[1];
                            break;

                        case "EndTime":
                            g_sEndTime = sConfigs[1];
                            break;

                        case "Interval":
                            g_iTimerInterval = Convert.ToInt32(sConfigs[1]);
                            break;

                        case "ProcInterval":
                            g_iProcInterval = Convert.ToInt32(sConfigs[1]);
                            break;

                        default:
                            break;
                    }
                }
            }

            g_sMessage = string.Format("[{0}] Configuration file read completed!!", c_sMethod);
            c_colib.cWriteLogs(g_sProcessor, g_sMessage);
        }

        private void PreProcessingJobs()
        {
            string c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            g_sMessage = string.Format("[{0}] DataTransferProc started ({1})!!", c_sMethod, c_covar.host_localcode);
            c_colib.cWriteLogs(g_sProcessor, g_sMessage);

            try
            {
                while (true)
                {
                    VerifyHostParameters();

                    if (g_bLocalVerified)
                    {
                        StartTimerOn();

                        if (c_localdb != null) c_localdb.DBClose();

                        return;
                    }
                    else
                    {
                        g_sMessage = string.Format("[{0}] Local Server is not verified!!", c_sMethod);
                        c_colib.cWriteLogs(g_sProcessor, g_sMessage);

                        Thread.Sleep(30000);
                    }
                }
            }
            catch (DataException ex)
            {
                g_sMessage = string.Format("[{0}] PreProcessingJobs error (line: {1})\n[{0}] {2}", c_sMethod, ex.Source.ToString(), ex.Message.ToString());
                c_colib.cWriteLogs(g_sProcessor, g_sMessage);
            }
            finally
            {
                if (c_localdb != null) c_localdb.DBClose();
            }
        }

        private void StartTimerOn()
        {
            string c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            _tmrLapse = new System.Timers.Timer();
            _tmrLapse.Elapsed += new ElapsedEventHandler(_tmrLapse_Elapsed);

            _tmrLapse.Interval = g_iTimerInterval; //number in milisecinds, 1000: 1 sec
            _tmrLapse.AutoReset = true;
            _tmrLapse.Enabled = true;
            _tmrLapse.Start();

            g_sMessage = string.Format("[{0}] Timer started!!", c_sMethod);
            c_colib.cWriteLogs(g_sProcessor, g_sMessage);
        }

        private void StartTimerOff()
        {
            string c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            _tmrLapse.Stop();
            _tmrLapse.Close();
            _tmrLapse.Enabled = false;

            g_sMessage = string.Format("[{0}] Timer off!!", c_sMethod);
            c_colib.cWriteLogs(g_sProcessor, g_sMessage);
        }

        protected override void OnStop()
        {
            string c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            _tmrLapse.Stop();
            _tmrLapse.Dispose();
            _tmrLapse = null;

            g_sMessage = string.Format("[{0}] Processor has been stopped!!", c_sMethod);
            c_colib.cWriteLogs(g_sProcessor, g_sMessage);
        }

        private void SetLocalDBInstance()
        {
            string c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            if (c_localdb != null) c_localdb = null;

            c_localdb = new cls_codb00();

            c_localdb.db_secure_key = g_localdbinfo.db_secure_key;
            c_localdb.db_host = g_localdbinfo.db_host;
            c_localdb.db_port = g_localdbinfo.db_port;
            c_localdb.db_name = g_localdbinfo.db_name;
            c_localdb.db_userid = g_localdbinfo.db_userid;
            c_localdb.db_password = g_localdbinfo.db_password;

            g_sMessage = string.Format("[{0}] Set local database instance!!", c_sMethod);
            c_colib.cWriteLogs(g_sProcessor, g_sMessage);
        }

        private void SetRemoteDBInstance()
        {
            string c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            if (c_remotedb != null) c_remotedb = null;

            c_remotedb = new cls_codb01();

            c_remotedb.db_host = g_remotedbinfo.db_host;
            c_remotedb.db_port = g_remotedbinfo.db_port;
            c_remotedb.db_name = g_remotedbinfo.db_name;
            c_remotedb.db_userid = g_remotedbinfo.db_userid;
            c_remotedb.db_password = g_remotedbinfo.db_password;

            g_sMessage = string.Format("[{0}] Set remote database instance!!", c_sMethod);
            c_colib.cWriteLogs(g_sProcessor, g_sMessage);
        }

        private void RemoveDBInstance()
        {
            c_localdb = null;
            c_remotedb = null;
        }

        private void VerifyHostParameters()
        {
            string c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string sQBuff = string.Empty;
            long iRecCount = 0L;

            g_bLocalVerified = false;

            SetLocalDBInstance();

            if (c_localdb.DBConnection() < 1)
            {
                g_bLocalVerified = false;

                g_sMessage = string.Format("[{0}] Local database connection failed ({1})!!", c_sMethod, c_localdb.db_host);
                c_colib.cWriteLogs(g_sProcessor, g_sMessage);

                return;
            }

            //Getting Local Server Information
            sQBuff = "SELECT bi_ename, bi_hostname " +
                       "FROM tb_branchinfo " +
                      "WHERE bi_cd = '" + c_covar.host_localcode + "' " +
                        "AND bi_hostname = '" + c_covar.host_localname + "' " +
                        "AND bi_active = '1'";

            iRecCount = c_localdb.RsOpen(sQBuff);

            if (iRecCount > 0 && c_localdb.rs.RecordCount > 0)
            {
                if (Convert.IsDBNull(c_localdb.rs.Fields["bi_hostname"].Value))
                {
                    g_sMessage = string.Format("[{0}] Local hostname is not identified ({1}, {2})!!", c_sMethod, c_covar.host_localcode, c_covar.host_localname);
                    c_colib.cWriteLogs(g_sProcessor, g_sMessage);
                }
                else
                {
                    c_localdb.RsClose();
                    g_bLocalVerified = true;

                    c_localdb.OpenSymmetricKey();

                    sQBuff = "SELECT bi_ename, bi_hostname, " +
                                    "CAST(DecryptByKey(bi_dbhost) AS varchar(100)) AS dbhost, " +
                                    "CAST(DecryptByKey(bi_dbport) AS varchar(100)) AS dbport, " +
                                    "CAST(DecryptByKey(bi_dbuserid) AS varchar(100)) AS dbuserid, " +
                                    "CAST(DecryptByKey(bi_dbpassword) AS varchar(100)) AS dbpassword, " +
                                    "CAST(DecryptByKey(bi_dbname) AS varchar(100)) AS dbname " +
                               "FROM tb_branchinfo " +
                              "WHERE bi_cd = '" + c_covar.host_remotecode + "' " +
                                "AND bi_hosttype = 'R' " +
                                "AND UPPER(bi_hostname) = '" + c_covar.host_remotename.ToUpper() + "' " +
                                "AND bi_active = '1'";

                    //for debugging usage
                    //g_sMessage = string.Format("[{0}] Remote database info query ({1})!!", c_sMethod, sQBuff);
                    //c_colib.cWriteLogs(g_sProcessor, g_sMessage);

                    iRecCount = c_localdb.RsOpen(sQBuff);

                    if (iRecCount > 0 && c_localdb.rs.RecordCount > 0)
                    {
                        g_remotedbinfo.db_host = Convert.ToString(c_localdb.rs.Fields["dbhost"].Value);
                        g_remotedbinfo.db_port = Convert.ToString(c_localdb.rs.Fields["dbport"].Value);
                        g_remotedbinfo.db_userid = Convert.ToString(c_localdb.rs.Fields["dbuserid"].Value);
                        g_remotedbinfo.db_password = Convert.ToString(c_localdb.rs.Fields["dbpassword"].Value);
                        g_remotedbinfo.db_name = Convert.ToString(c_localdb.rs.Fields["dbname"].Value);

                        SetRemoteDBInstance();

                        //for debugging usage
                        //g_sMessage = string.Format("[{0}] Remote database info ({1}, {2}, {3}, {4}, {5})!!", c_sMethod, g_remotedbinfo.db_host, g_remotedbinfo.db_port, g_remotedbinfo.db_userid, g_remotedbinfo.db_password, g_remotedbinfo.db_name);
                        //c_colib.cWriteLogs(c_sProcessor, "DB Info: " + c_remotedb.db_host + c_remotedb.db_name + Convert.ToString(c_localdb.rs.Fields["dbuserid"].Value) + Convert.ToString(c_localdb.rs.Fields["dbpassword"].Value) + " (" + c_sMethod + ")!!");
                        //c_colib.cWriteLogs(g_sProcessor, g_sMessage);
                    }
                    else
                    {
                        g_bRemoteConnection = false;

                        g_sMessage = string.Format("[{0}] Retrieving remote database info failed ({1})!!", c_sMethod, iRecCount.ToString());
                        c_colib.cWriteLogs(g_sProcessor, g_sMessage);
                    }

                    c_localdb.CloseSymmetricKey();
                }
            }
            else
            {
                g_bLocalVerified = false;

                g_sMessage = string.Format("[{0}] Local hostname does not exist!!", c_sMethod);
                c_colib.cWriteLogs(g_sProcessor, g_sMessage);
            }

            c_localdb.RsClose();
            c_localdb.DBClose();
        }

        private void _tmrLapse_Elapsed(object source, EventArgs e)
        {
            string c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            //for debugging usage
            //g_sMessage = string.Format("[{0}] Timer elapsed!!", c_sMethod);
            //c_colib.cWriteLogs(g_sProcessor, g_sMessage);

            try
            {
                if (g_sStartDate.CompareTo(DateTime.Now.ToString("yyyy-MM-dd")) <= 0 && g_sEndDate.CompareTo(DateTime.Now.ToString("yyyy-MM-dd")) >= 0)
                {
                    if ((!g_bTimeOn && g_sStartTime.CompareTo(DateTime.Now.ToString("HH:mm:ss")) == 0) || (g_bStartOn && g_sStartTime.CompareTo(DateTime.Now.ToString("HH:mm:ss")) < 0))
                    {
                        g_sMessage = string.Format("[{0}] Scheduler On!!", c_sMethod);
                        c_colib.cWriteLogs(g_sProcessor, g_sMessage);

                        g_bTimeOn = true;
                        g_bStartOn = false;                 // 첫 실행 여부 판단
                        g_bFinished = false;

                        g_bStopped = true;

                        SetLocalDBInstance();
                        SetRemoteDBInstance();

                        _bgwDataProcessors = new BackgroundWorker();
                        InitializeBackgroundWorker();
                        _bgwDataProcessors.RunWorkerAsync(1);                       // _bgwDataProcessors_DoWork 프로세스 트리거

                        _tmrLapse.Stop();
                        _tmrLapse.Close();

                        GetTimerInfo();

                        _tmrLapse.Start();

                        SendAlertMessage("Processor started");
                    }

                    if (g_bTimeOn && g_sEndTime.CompareTo(DateTime.Now.ToString("HH:mm:ss")) < 0)
                    {
                        _bgwDataProcessors.CancelAsync();
                        _bgwDataProcessors = null;

                        g_bTimeOn = false;
                        g_bFinished = true;

                        RemoveDBInstance();

                        SendAlertMessage("Processor finished");

                        //for debugging usage
                        //GetTimerInfo();
                    }
                }
                else
                {
                    g_sMessage = string.Format("[{0}] The schedule does not available. Check schedule date!!", c_sMethod);
                    c_colib.cWriteLogs(g_sProcessor, g_sMessage);
                }
            }
            catch(Exception ex)
            {
                g_sMessage = string.Format("[{0}] Exception error caught (line: {1})\n[{0}] {2}", c_sMethod, ex.Source.ToString(), ex.Message.ToString());
                c_colib.cWriteLogs(g_sProcessor, g_sMessage);
            }
            finally
            {
            }
        }

        private void SendAlertMessage(string msg)
        {
            c_emaillib.EmailSubject = "[" + c_covar.host_localname + "] " + g_sProcessor + " Processor alert";
            c_emaillib.EmailBody = msg;
            c_emaillib.SendEmail();
        }

        private void InitializeBackgroundWorker()
        {
            _bgwDataProcessors.WorkerSupportsCancellation = true;

            _bgwDataProcessors.DoWork += new DoWorkEventHandler(_bgwDataProcessors_DoWork);
            _bgwDataProcessors.RunWorkerCompleted += new RunWorkerCompletedEventHandler(_bgwDataProcessors_RunWorkerCompleted);
            _bgwDataProcessors.ProgressChanged += new ProgressChangedEventHandler(_bgwDataProcessors_ProgressChanged);
        }

        private void _bgwDataProcessors_DoWork(object sender, DoWorkEventArgs e)
        {
            string c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name; ;

            BackgroundWorker worker = sender as BackgroundWorker;

            g_sMessage = string.Format("[{0}] DoWork triggered!!", c_sMethod);
            c_colib.cWriteLogs(g_sProcessor, g_sMessage);

            // Assign the result of the computation to the Result property of the DoWorkEventArgs object. This is will be available to the RunWorkerCompleted eventhandler.
            e.Result = RunDataSyncProcessors((int)e.Argument, worker, e);
        }

        //private void TransferMemberDataToWebApp()
        private void TransferMemberDataToWebApp(Object state)
        {
            string c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            string s_qbuff = string.Empty, s_dqquery = string.Empty;
            string sMessage = string.Empty;
            Recordset rs_dqrecordset = new Recordset();

            long lErrorCode = 0;
            var thrCurrentProcess = Thread.CurrentThread;

            rs_dqrecordset = new Recordset();

            //for debugging usage
            //g_sMessage = string.Format("[{0}] Transferring data procedure started (Thread #{1})!!", c_sMethod, thrCurrentProcess.ManagedThreadId);
            //c_colib.cWriteLogs(g_sProcessor, g_sMessage);

            try
            {
                if (c_localdb.DBConnection() < 1)
                {
                    g_sMessage = string.Format("[{0}] Local database connection failed!!", c_sMethod);
                    c_colib.cWriteLogs(g_sProcessor, g_sMessage);

                    _Semaphore.Release();

                    g_bStopped = true;

                    return;
                }

                _Semaphore.Wait();

                s_qbuff = "SELECT dq_seq, dq_trandate, dq_trantime, dq_source, dq_destination, dq_dbname, dq_key, dq_dmltype, dq_dbquery, dq_target_range, dq_datetime, dq_trigger, dq_error " +
                                "FROM tb_syncdataque " +
                               "WHERE dq_destination = '" + c_covar.host_remotecode + "' " +
                                 "AND dq_target_range = '1' " +
                                 "AND dq_error IS NULL " +
                               "ORDER BY dq_trandate, dq_trantime";

                //for debugging usage
                //g_sMessage = string.Format("[{0}] Que data query ({1})!!", c_sMethod, s_qbuff);
                //c_colib.cWriteLogs(g_sProcessor, g_sMessage);

                if (c_localdb.RsOpen(ref rs_dqrecordset, s_qbuff) > 0 && rs_dqrecordset.RecordCount > 0)
                {
                    //for debugging usage
                    //g_sMessage = string.Format("[{0}] Que data record count ({1})!!", c_sMethod, rs_dqrecordset.RecordCount.ToString());
                    //c_colib.cWriteLogs(g_sProcessor, g_sMessage);

                    if (c_remotedb.DBConnection() < 1)
                    {
                        g_sMessage = string.Format("[{0}] Remote database connection failed ({1}, {2})\n[{0}] {3}!!", c_sMethod, c_covar.host_remotecode, c_remotedb.db_host, c_remotedb.error_message);
                        c_colib.cWriteLogs(g_sProcessor, g_sMessage);

                        _Semaphore.Release();
                        g_bStopped = true;

                        return;
                    }

                    while (!rs_dqrecordset.EOF)
                    {
                        s_dqquery = string.Format(rs_dqrecordset.Fields["dq_dbquery"].Value.ToString(), rs_dqrecordset.Fields["dq_dbname"].Value);
                        s_qbuff = s_dqquery.Replace("''", "'");
                        s_qbuff = s_dqquery.Replace(", , , ", "");

                        lErrorCode = c_remotedb.DBExcute(s_qbuff);

                        if(lErrorCode < 0)
                        {
                            //Error 처리 로직 변경: 2021.06.04 by Robin
                            //Error Data 별도 테이블에 Insert, 기존 데이터는 그대로 놔 둠.
                            //rs_dqrecordset.Fields["dq_error"].Value = c_remotedb.error_message;
                            //rs_dqrecordset.Update();

                            InstErrorData(Convert.ToInt64(rs_dqrecordset.Fields["dq_seq"].Value));
                            //Error 처리 로직 변경: 2021.06.04 by Robin

                            sMessage = string.Format("[{0}] Query execute error ({1})\n[{0}] {2}", c_sMethod, s_qbuff, c_remotedb.error_message);
                            c_colib.cWriteLogs(g_sProcessor, sMessage);
                        }
                        else
                        {
                            rs_dqrecordset.Delete();

                            //sMessage = string.Format("[{0}] Affected data count: {1}", c_sMethod, c_remotedb.affectedrecords);
                            //c_colib.cWriteLogs(g_sProcessor, sMessage);
                        }

                        rs_dqrecordset.MoveNext();
                    }
                }
                else
                {
                    //g_sMessage = string.Format("[{0}] Que data not found ({1})!!", c_sMethod, rs_dqrecordset.RecordCount.ToString());
                    //c_colib.cWriteLogs(g_sProcessor, sMessage);
                }

                c_localdb.RsClose(ref rs_dqrecordset);

                rs_dqrecordset = null;
            }
            catch (MySqlException ex)
            {
                sMessage = string.Format("[{0}] MySql exception error caught (line: {1})\n[{0}] {2}", c_sMethod, ex.Source.ToString(), c_remotedb.error_message);
                c_colib.cWriteLogs(g_sProcessor, sMessage);
            }
            catch (Exception e)
            {
                sMessage = string.Format("[{0}] Query exception error caught (line: {1})\n[{0}] {2}", c_sMethod, e.Source.ToString(), e.Message.ToString());
                c_colib.cWriteLogs(g_sProcessor, sMessage);
            }
            finally
            {
                //c_remotedb = null;
                g_bStopped = true;

                _Semaphore.Release();
            }
        }

        private void InstErrorData(long vSeqNo)
        {
            string s_qbuff = string.Empty;
            string sMessage = string.Empty;
            string c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            try
            {
                s_qbuff = "INSERT INTO tb_syncdataque_error (dq_trandate, dq_trantime, dq_source, dq_destination, dq_dbname, dq_key, dq_dmltype, dq_dbquery, dq_target_range, dq_datetime, dq_trigger, dq_ref, dq_error) " +
                                "SELECT dq_trandate, dq_trantime, dq_source, dq_destination, dq_dbname, dq_key, dq_dmltype, dq_dbquery, dq_target_range, dq_datetime, dq_trigger, " + vSeqNo.ToString() + ", '" + c_remotedb.error_message + "' " +
                                  "FROM tb_syncdataque " +
                                 "WHERE dq_seq = " + vSeqNo;

                c_localdb.DBExcute(s_qbuff);
            }
            catch (Exception e)
            {
                sMessage = string.Format("[{0}] Query exception error caught (line: {1})\n[{0}] {2}", c_sMethod, e.Source.ToString(), e.Message.ToString());
                c_colib.cWriteLogs(g_sProcessor, sMessage);
            }
        }

        private long RunDataSyncProcessors(int n, BackgroundWorker worker, DoWorkEventArgs e)
        {
            string c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            g_sMessage = string.Format("[{0}] Backgoundwork started", c_sMethod);
            c_colib.cWriteLogs(g_sProcessor, g_sMessage);

            try
            {
                if (worker.CancellationPending)
                {
                    e.Cancel = true;

                    g_sMessage = string.Format("[{0}] Work cancelled", c_sMethod);
                    c_colib.cWriteLogs(g_sProcessor, g_sMessage);
                }
                else
                {
                    g_bStopped = true;

                    if (_Semaphore != null)
                    {
                        _Semaphore = null;
                    }

                    _Semaphore = new SemaphoreSlim(1);

                    while (true)                     // Main Process will be finished at a certain time from scheduler
                    {
                        if (g_bFinished)
                        {
                            if (!g_bStopped)
                                c_colib.cWriteLogs(g_sProcessor, "Jobs finished (" + c_sMethod + ")");

                            Thread.Sleep(60000);
                            //GetTimerInfo();
                        }
                        else
                        {
                            if (g_bStopped)
                            {
                                g_bStopped = false;

                                ThreadPool.QueueUserWorkItem(TransferMemberDataToWebApp);
                            }
                            else
                            {
                                g_sMessage = string.Format("[{0}] Processor still alive", c_sMethod);
                                c_colib.cWriteLogs(g_sProcessor, g_sMessage);
                            }
                        }

                        Thread.Sleep(g_iProcInterval);
                    }
                }
            }
            catch (Exception ex)
            {
                g_sMessage = string.Format("[{0}] Exception error caught (line: {1})\n[{0}] {2}", c_sMethod, ex.Source.ToString(), ex.Message.ToString());
                c_colib.cWriteLogs(g_sProcessor, g_sMessage);
            }
            finally
            {
                g_bStopped = true;

                g_sMessage = string.Format("[{0}] Processor exit", c_sMethod);
                c_colib.cWriteLogs(g_sProcessor, g_sMessage);
            }

            return 0;
        }

        private void _bgwDataProcessors_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            string c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            if (e.Error != null)
            {
                g_bTimeOn = false;
                g_bStartOn = true;

                g_sMessage = string.Format("[{0}] Work completed with error\n[{0}] {1}", c_sMethod, e.Error.ToString());
                c_colib.cWriteLogs(g_sProcessor, g_sMessage);
            }
            else if (e.Cancelled)
            {
                g_sMessage = string.Format("[{0}] Work completed with cancel\n[{0}] {1}", c_sMethod, e.Error.ToString());
                c_colib.cWriteLogs(g_sProcessor, g_sMessage);
            }
            else
            {
                // Finally, handle the case where the operation succeeded.
                g_sMessage = string.Format("[{0}] Work completed with success", c_sMethod);
                c_colib.cWriteLogs(g_sProcessor, g_sMessage);
            }
        }

        // This event handler updates the progress bar.
        private void _bgwDataProcessors_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            string c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            g_sMessage = string.Format("[{0}] Processing changed", c_sMethod);
            c_colib.cWriteLogs(g_sProcessor, g_sMessage);
        }
    }
}
