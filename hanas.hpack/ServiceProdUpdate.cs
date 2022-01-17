using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Timers;

using hanas.covar;
using hanas.codb;
using hanas.colibs;
using hanas.emaillibs;


namespace lemond.hanastt
{
    public partial class ServiceProdUpdate : ServiceBase
    {
        private cls_codb00 c_sttdb = new cls_codb00();
        private cls_codb00 c_amdb = new cls_codb00();

        private cls_covar00 c_covar = new cls_covar00();
        private cls_colibs00 c_colib = new cls_colibs00();
        private cls_emaillibs00 c_emaillib = new cls_emaillibs00();

        private ADODB.Connection c_stt_dbcon = new ADODB.Connection();
        private ADODB.Connection c_am_dbcon = new ADODB.Connection();

        private ADODB.Recordset c_stt_rs = new ADODB.Recordset();
        private ADODB.Recordset c_am_rs = new ADODB.Recordset();

        private Timer c_tmrLapse;

        private string c_sSTTDBString = "Provider=SQLOLEdb.1;uid=sa;password=Lemonto33!!;database=HanaSTT;Data Source=10.31.0.10,1433;";
        private string c_sAMDBString = "Provider=SQLOLEdb.1;uid=sa;password=go;database=lemond;Data Source=10.31.0.15,1433;";
        //private string c_sSTTDBString = "Provider=SQLOLEdb.1;uid=sa;password=gkskgksk;database=HanaSTT;Data Source=10.0.1.1,1433;";
        //private string c_sAMDBString = "Provider=SQLOLEdb.1;uid=sa;password=gkskgksk;database=lemond;Data Source=10.0.1.1,1433;";

        private string c_sMessage = string.Empty;
        private string c_sProcessor = System.AppDomain.CurrentDomain.FriendlyName.Replace(".exe", "");

        private string c_sStartDate = DateTime.Now.ToString("yyyy-MM-dd");
        private string c_sStartTime = "07:00:00";
        private string c_sEndDate = "2999-12-31";
        private string c_sEndTime = "19:00:00";
        private int c_iInterval = 1;         // Default 1 minute

        private bool c_bTimeOn = false, c_bStartOn = true;

        public ServiceProdUpdate()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            c_sttdb.db_params = c_sSTTDBString;
            c_amdb.db_params = c_sAMDBString;

            c_sMessage = "Process was started";
            c_colib.cWriteLogs(c_sProcessor, c_sMessage);

            c_tmrLapse = new Timer();
            c_tmrLapse.Elapsed += new ElapsedEventHandler(PerformOperations);
            c_tmrLapse.Interval = c_iInterval * 1000; //number in milisecinds, 1000: 1 sec
            c_tmrLapse.AutoReset = true;
            c_tmrLapse.Enabled = true;
            c_tmrLapse.Start();

            GetConfigInfo();
        }

        protected override void OnStop()
        {
            c_tmrLapse.Stop();
            c_tmrLapse.Dispose();
            c_tmrLapse = null;

            c_sttdb.DBClose();
            c_amdb.DBClose();

            c_stt_dbcon = null;
            c_am_dbcon = null;

            c_covar = null;
            c_sttdb = null;
            c_amdb = null;

            c_sMessage = "Process has been stoppted";
            c_colib.cWriteLogs(c_sProcessor, c_sMessage);
        }

        private void GetConfigInfo()
        {
            int iListCount = 0, iReturn = 0;
            string[] sConfigs;
            List<string> lLine = new List<string>();

            //iReturn = c_colib.cReadFile("lemond.hanastt", ref lLine);
            iReturn = c_colib.cReadFile(c_sProcessor, ref lLine);

            if (iReturn != 1)
            {
                c_sMessage = "Config file Error";
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
                            c_iInterval = Convert.ToInt32(sConfigs[1]);
                            break;

                        default:
                            break;
                    }
                }
            }

            c_sMessage = "Configuration file read completed";
            c_colib.cWriteLogs(c_sProcessor, c_sMessage);
        }

        private void StopProcess()
        {
            this.Stop();
            Environment.Exit(0);

            c_sMessage = "Stopping";
            c_colib.cWriteLogs(c_sProcessor, c_sMessage);

            return;
           
        }

        private void PerformOperations(object source, EventArgs e)
        {
            string sBodyMessage = string.Empty;

            try
            {
                if (c_sStartDate.CompareTo(DateTime.Now.ToString("yyyy-MM-dd")) <= 0 && c_sEndDate.CompareTo(DateTime.Now.ToString("yyyy-MM-dd")) >= 0)
                {
                    if ((!c_bTimeOn && c_sStartTime.CompareTo(DateTime.Now.ToString("HH:mm:ss")) == 0) || (c_bStartOn && c_sStartTime.CompareTo(DateTime.Now.ToString("HH:mm:ss")) < 0))
                    {
                        c_bTimeOn = true;
                        c_bStartOn = false;

                        c_tmrLapse.Stop();
                        c_tmrLapse.Close();
                        //c_tmrLapse.Dispose();
                        //c_tmrLapse = null;

                        //c_tmrLapse = new Timer();

                        c_tmrLapse.Interval = c_iInterval * 1000;
                        //c_tmrLapse.AutoReset = true;
                        c_tmrLapse.Enabled = true;
                        c_tmrLapse.Start();
                    }

                    if (c_bTimeOn && c_sEndTime.CompareTo(DateTime.Now.ToString("HH:mm:ss")) < 0)
                    {
                        c_bTimeOn = false;

                        c_tmrLapse.Stop();
                        c_tmrLapse.Close();
                        //c_tmrLapse.Dispose();
                        //c_tmrLapse = null;

                        //c_tmrLapse = new Timer();

                        c_tmrLapse.Interval = 1000;
                        //c_tmrLapse.AutoReset = true;
                        c_tmrLapse.Enabled = true;
                        c_tmrLapse.Start();
                    }

                    if (c_bTimeOn) {
                        //var TaskUpdateProductInfo = Task.Factory.StartNew(() => UpdateProductInfo());
                        UpdateProductInfo();
                        sBodyMessage = sBodyMessage + "<br />" + c_sMessage;

                        //var TaskUpdatePromotionItemInfo = Task.Factory.StartNew(() => UpdatePromotionItemInfo());
                        UpdatePromotionItemInfo();
                        sBodyMessage = sBodyMessage + "<br />" + c_sMessage;
                    }
                }
                else
                {
                    c_sMessage = "Please check the available date";
                    c_colib.cWriteLogs(c_sProcessor, c_sMessage);
                }
            }
            catch
            {
                sBodyMessage = sBodyMessage + "<br />" + c_sMessage;
            }
            finally
            {
                if (c_bTimeOn)
                {
                    c_emaillib.EmailSubject = "[" + c_covar.DeviceName + "] Auto Processor alert";
                    c_emaillib.EmailBody = sBodyMessage;
                    c_emaillib.SendEmail();
                }

                //if (bStartOn)
                //{
                //    c_tmrLapse.Stop();
                //    c_tmrLapse.Enabled = false;
                //    c_tmrLapse.Interval = c_iInterval * 1000;
                //    c_tmrLapse.AutoReset = true;
                //    c_tmrLapse.Enabled = true;
                //    c_tmrLapse.Start();
                //}
            }
        }

        private void UpdateProductInfo()
        {
            int iReturn = 0, iTotal = 0, iUpdated = 0, iSkipped = 0;

            string sQuery = string.Empty, sProdCode = string.Empty;
            string sProcedure = System.Reflection.MethodBase.GetCurrentMethod().Name;

            try
            {
                if (c_sttdb.DBConnection(ref c_stt_dbcon) < 1)
                {
                    c_sMessage = "(" + sProcedure + ") STTDB: " + c_sttdb.error_message;
                    iReturn = -1;
                }
                else
                {
                    sQuery = "SELECT pd_code, pd_saleprice, pd_onhand, pd_stockdate, pd_uid, pd_udate, pd_utime " +
                               "FROM tb_product " +
                              "WHERE pd_type = '3' " +
                                "AND pd_status = '1'";

                    if (c_sttdb.RsOpen(ref c_stt_dbcon, ref c_stt_rs, sQuery) >= 0)
                    {
                        iTotal = c_stt_rs.RecordCount;

                        c_sMessage = "(" + sProcedure + ") STTDB: " + iTotal.ToString() + " items retrived";
                        c_colib.cWriteLogs(c_sProcessor, c_sMessage);

                        if (c_amdb.DBConnection(ref c_am_dbcon) < 1)
                        {
                            iReturn = -1;
                            c_sMessage = "(" + sProcedure + ") " + c_amdb.error_message;
                        }
                        else
                        {
                            while (!c_stt_rs.EOF)
                            {
                                sProdCode = c_stt_rs.Fields["pd_code"].Value.ToString().Trim();

                                sQuery = "SELECT ISNULL(y.nprice, 0) AS price, " +
                                                "ISNULL((SELECT TOP 1 ISNULL(x.nonhand, 0) FROM iciwhs x WHERE x.citemno = y.citemno AND x.citemno <> ''), 0) AS onhand " +
                                           "FROM icitem y " +
                                          "WHERE RTRIM(y.citemno) = '" + sProdCode + "'";

                                if (c_amdb.RsOpen(ref c_am_dbcon, ref c_am_rs, sQuery) >= 0)
                                {
                                    if (c_am_rs.RecordCount > 0)
                                    {
                                        c_stt_rs.Fields["pd_saleprice"].Value = Convert.ToSingle(c_am_rs.Fields["price"].Value.ToString() == "" ? "0" : c_am_rs.Fields["price"].Value);
                                        c_stt_rs.Fields["pd_onhand"].Value = Convert.ToSingle(c_am_rs.Fields["onhand"].Value.ToString() == "" ? "0" : c_am_rs.Fields["onhand"].Value);
                                        c_stt_rs.Fields["pd_stockdate"].Value = DateTime.Now.ToString("yyyy-MM-dd");
                                        c_stt_rs.Fields["pd_uid"].Value = c_covar.DeviceName;
                                        c_stt_rs.Fields["pd_udate"].Value = DateTime.Now.ToString("yyyy-MM-dd");
                                        c_stt_rs.Fields["pd_utime"].Value = DateTime.Now.ToString("HH:mm:ss");

                                        c_stt_rs.Update();

                                        iUpdated++;
                                        //c_sMessage = "STTDB: (" + sProdCode + ") item was updated";
                                        //c_colib.cWriteLogs(sProcessor, c_sMessage);
                                    }
                                    else
                                    {
                                        iSkipped++;

                                        c_sMessage = "(" + sProcedure + ") AMDB: (" + sProdCode + ") item was not found";
                                        c_colib.cWriteLogs(c_sProcessor, c_sMessage);
                                    }

                                    c_amdb.RsClose(ref c_am_rs);
                                }
                                else    // c_amdb.RsOpen()
                                {
                                    iReturn = -1;

                                    c_sMessage = "(" + sProcedure + ") AMDB: " + c_amdb.error_message;
                                    c_colib.cWriteLogs(c_sProcessor, c_sMessage);
                                }

                                c_stt_rs.MoveNext();
                            }           // while(!c_stt_rs.EOF)

                            c_sttdb.RsClose(ref c_stt_rs);

                            c_sMessage = "(" + sProcedure + ") STTDB: The process has been done(" + iUpdated.ToString() + " updated, " + iSkipped.ToString() + " skipped)";
                        }
                    }
                    else                // c_sttdb.RsOpen()
                    {
                        iReturn = -1;
                        c_sMessage = "(" + sProcedure + ") STTDB: " + c_sttdb.error_message;
                    }
                }

            }
            catch (Exception ex)
            {
                iReturn = -1;
                c_sMessage = "(" + sProcedure + ") " + ex.Message.ToString();
            }
            finally
            {
                c_sttdb.RsClose(ref c_stt_rs);
                c_amdb.RsClose(ref c_am_rs);

                c_sttdb.DBClose();
                c_amdb.DBClose();

                c_colib.cWriteLogs(c_sProcessor, c_sMessage);

                if (iReturn == -1)
                {
                    c_emaillib.EmailSubject = "[" + c_covar.DeviceName + "] (" + sProcedure + ") Auto Processor alert";
                    c_emaillib.EmailBody = c_sMessage;
                    c_emaillib.SendEmail();
                }
            }
        }


        private void UpdatePromotionItemInfo()
        {
            int iReturn = 0, iTotal = 0, iUpdated = 0, iSkipped = 0;

            string sQuery = string.Empty, sProdCode = string.Empty;
            string sProcedure = System.Reflection.MethodBase.GetCurrentMethod().Name;

            try
            {
                if (c_sttdb.DBConnection(ref c_stt_dbcon) < 1)
                {
                    c_sMessage = "(" + sProcedure + ") STTDB: " + c_sttdb.error_message;
                    iReturn = -1;
                }
                else
                {
                    sQuery = "SELECT pm_code, pm_onhand, pm_stockdate, pm_uid, pm_udate, pm_utime " +
                               "FROM tb_promotion " +
                              "WHERE pm_status = '1'";

                    if (c_sttdb.RsOpen(ref c_stt_dbcon, ref c_stt_rs, sQuery) >= 0)
                    {
                        iTotal = c_stt_rs.RecordCount;

                        c_sMessage = "(" + sProcedure + ") STTDB: " + iTotal.ToString() + " items retrived";
                        c_colib.cWriteLogs(c_sProcessor, c_sMessage);

                        if (c_amdb.DBConnection(ref c_am_dbcon) < 1)
                        {
                            iReturn = -1;
                            c_sMessage = "(" + sProcedure + ") " + c_amdb.error_message;
                        }
                        else
                        {
                            while (!c_stt_rs.EOF)
                            {
                                sProdCode = c_stt_rs.Fields["pm_code"].Value.ToString().Trim();

                                sQuery = "SELECT ISNULL(nonhand, 0) AS onhand " +
                                           "FROM iciwhs " +
                                          "WHERE citemno <> '' " +
                                            "AND citemno IS NOT NULL " +
                                            "AND RTRIM(citemno) = '" + sProdCode + "'";

                                if (c_amdb.RsOpen(ref c_am_dbcon, ref c_am_rs, sQuery) >= 0)
                                {
                                    if (c_am_rs.RecordCount > 0)
                                    {
                                        c_stt_rs.Fields["pm_onhand"].Value = Convert.ToSingle(c_am_rs.Fields["onhand"].Value.ToString() == "" ? "0" : c_am_rs.Fields["onhand"].Value);
                                        c_stt_rs.Fields["pm_stockdate"].Value = DateTime.Now.ToString("yyyy-MM-dd");
                                        c_stt_rs.Fields["pm_uid"].Value = c_covar.DeviceName;
                                        c_stt_rs.Fields["pm_udate"].Value = DateTime.Now.ToString("yyyy-MM-dd");
                                        c_stt_rs.Fields["pm_utime"].Value = DateTime.Now.ToString("HH:mm:ss");

                                        c_stt_rs.Update();

                                        iUpdated++;
                                        //c_sMessage = "STTDB: (" + sProdCode + ") item was updated";
                                        //c_colib.cWriteLogs(sProcessor, c_sMessage);
                                    }
                                    else
                                    {
                                        iSkipped++;

                                        c_sMessage = "(" + sProcedure + ") AMDB: (" + sProdCode + ") item was not found";
                                        c_colib.cWriteLogs(c_sProcessor, c_sMessage);
                                    }

                                    c_amdb.RsClose(ref c_am_rs);
                                }
                                else    // c_amdb.RsOpen()
                                {
                                    iReturn = -1;

                                    c_sMessage = "(" + sProcedure + ") AMDB: " + c_amdb.error_message;
                                    c_colib.cWriteLogs(c_sProcessor, c_sMessage);
                                }

                                c_stt_rs.MoveNext();
                            }           // while(!c_stt_rs.EOF)

                            c_sttdb.RsClose(ref c_stt_rs);

                            c_sMessage = "(" + sProcedure + ") STTDB: The process has been done(" + iUpdated.ToString() + " updated, " + iSkipped.ToString() + " skipped)";
                        }
                    }
                    else                // c_sttdb.RsOpen()
                    {
                        iReturn = -1;
                        c_sMessage = "(" + sProcedure + ") STTDB: " + c_sttdb.error_message;
                    }
                }
            }
            catch (Exception ex)
            {
                iReturn = -1;
                c_sMessage = "(" + sProcedure + ") " + sProdCode + ": " + ex.Message.ToString();
            }
            finally
            {
                c_sttdb.RsClose(ref c_stt_rs);
                c_amdb.RsClose(ref c_am_rs);

                c_sttdb.DBClose();
                c_amdb.DBClose();

                c_colib.cWriteLogs(c_sProcessor, c_sMessage);

                if (iReturn == -1)
                {
                    c_emaillib.EmailSubject = "[" + c_covar.DeviceName + "] (" + sProcedure + ") Auto Processor alert";
                    c_emaillib.EmailBody = c_sMessage;
                    c_emaillib.SendEmail();
                }
            }
        }


        //private void subSendEmail()
        //{
        //    string sReceipient = string.Empty;
        //    string sSubject = string.Empty;

        //    MailMessage mEmail = new MailMessage();

        //    string sProcedure = System.Reflection.MethodBase.GetCurrentMethod().Name;

        //    sReceipient = "Robin Hahm" + " <robin.hahm@hanasolution.com>";
        //    sSubject = "Password from Hanas Order System - Do not reply!!";

        //    mEmail.From = new MailAddress("alert@hanasolution.com", "Hana Solution Alert");
        //    mEmail.To.Add(sReceipient);
        //    mEmail.Bcc.Add("Hana Solution Inc <support@hanasolution.com>");
        //    mEmail.Subject = sSubject;
        //    mEmail.Body = "<html><body><div>" +
        //                  "Hi Robin," + "<br />" +
        //                  "" + "<br />" +
        //                  "Your user information is the below: " + "<br />" +
        //                  "" + "<br />" +
        //                  " User ID: robin.hahm<br />" +
        //                  " Password: test<br />" +
        //                  "" + "<br />" +
        //                  "Hana Solution Inc." + "<br />" +
        //                  "419-1952 Kingsway Ave, Port Coquitlam, BC  V3C 6C2" + "<br />" +
        //                  "" + "<br />" +
        //                  "TEL : (+)1-778-285-2255, 1-844-826-2255" + "<br />" +
        //                  "Email : support@hanasolution.com" + "<br />" +
        //                  "</div></body></html>";

        //    mEmail.IsBodyHtml = true;

        //    SmtpClient SmtpServer = new SmtpClient("mail.hanasolution.com");
        //    //SmtpClient SmtpServer = new SmtpClient("smtpout.secureserver.net");
        //    //SmtpClient SmtpServer = new SmtpClient("a2plcpnl0838.prod.iad2.secureserver.net");
        //    SmtpServer.Port = 25;
        //    //SmtpServer.Port = 465;
        //    SmtpServer.Credentials = new System.Net.NetworkCredential("support@hanasolution.com", "v4pT_yX(AFd4");
        //    SmtpServer.EnableSsl = false;
        //    //SmtpServer.EnableSsl = true;

        //    SmtpServer.DeliveryMethod = SmtpDeliveryMethod.Network;

        //    try
        //    {
        //        SmtpServer.Send(mEmail);
        //    }
        //    catch (Exception ex)
        //    {
        //        c_sMessage = ex.Message.ToString();
        //        c_colib.cWriteLogs(sProcessor, c_sMessage);
        //    }
        //}

        //private void UpdatePromotionItemInfo()
        //{
        //StreamWriter swr = new StreamWriter("c:\\test_from_database.txt",true);

        //try
        //{
        //OdbcConnection con = new OdbcConnection("DSN=liquor_data");

        //OdbcDataAdapter adp = new OdbcDataAdapter("", con);

        //DataSet ds = new DataSet();

        //string sql = "select * from item_group";
        //adp.SelectCommand.CommandText = sql;

        //adp.Fill(ds, "item_group");

        //foreach (DataRow dr in ds.Tables["item_group"].Rows)
        //{
        //    //      swr.Write(dr["group_name"].ToString() + "\t\t" + DateTime.Now.TimeOfDay.ToString() + "\n");

        //    //Console.WriteLine(dr["group_name"].ToString() + "\t\t" + DateTime.Now.TimeOfDay.ToString() + "\n");
        //    m_streamWriter.WriteLine(dr["group_name"].ToString() + "\t\t" + DateTime.Now.TimeOfDay.ToString() + "\n");
        //}

        //m_streamWriter.Flush();
        //}

        //catch (Exception ex)
        //{
        // swr.Write("Error :"+ ex.Message + "\t\t" + DateTime.Now.TimeOfDay.ToString() + "\n"); }
        //    }
        //}

        //public void WriteLogs(string Message)
        //{
        //    string sAppPath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\";

        //    if (!Directory.Exists(sAppPath))
        //    {
        //        Directory.CreateDirectory(sAppPath);
        //    }

        //    string filepath = sAppPath + System.AppDomain.CurrentDomain.FriendlyName.Replace(".exe", "") + "_" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";

        //    if (!File.Exists(filepath))
        //    {
        //        // Create a file to write to.   
        //        using (StreamWriter sw = File.CreateText(filepath))
        //        {
        //            sw.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + Message);
        //        }
        //    }
        //    else
        //    {
        //        using (StreamWriter sw = File.AppendText(filepath))
        //        {
        //            sw.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + Message);
        //        }
        //    }
        //}
    }
}
