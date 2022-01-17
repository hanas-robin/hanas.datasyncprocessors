namespace hanas.datasynccontroller
{
    using System;
    using System.Threading;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using ADODB;
        
    using hanas.com.covar;
    using hanas.com.colibs;
    using hanas.com.codb;


    internal partial class Program
    {
        private static SemaphoreSlim _Semaphore;

        private static cls_covar00 c_covar = new cls_covar00();
        private static cls_codb00 c_localdb;
        private static cls_codb00[] c_remotedb;

        //static string c_qbuff = string.Empty;

        private static string c_sProcessor = System.AppDomain.CurrentDomain.FriendlyName.Replace(".exe", "");
        private static string c_sMethod;

        private static cls_colibs00 c_colib = new cls_colibs00();

        private static string[] c_sBranch;


        private static void Main(string[] args)
        {
            int i = 0;
            string s_qbuff;

            AppDomain.CurrentDomain.ProcessExit += Destructor;

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
                c_colib.cWriteLogs(c_sProcessor, "Please Check Parameters (" + c_sMethod + ")!!");

                return;
            }

            //c_colib.cWriteLogs(c_sProcessor, "Processor started (" + c_sMethod + ")!!");

            try
            {
                c_localdb = new cls_codb00();
                c_localdb.subSetSysDatabaseInformation();

                if (c_localdb.DBConnection() < 1)
                {
                    c_colib.cWriteLogs(c_sProcessor, "Database connection failed (" + c_sMethod + ")!!");
                    return;
                }
                else
                {
                    //c_colib.cWriteLogs(c_sProcessor, "Database connection extablished (" + c_sMethod + ")!!");
                }

                c_localdb.OpenSymmetricKey();

                // Getting Remote Server Information. Branch List if Server Daemon vs Server Info if Client Daemon
                // 각 매장 서버 디비정보 셋팅: hosttype = 'R' 의 경우, tfCollection, mfCust 트리거 작동시 본부 서버 업로드용으로 레코드 생성 (단, R 타입의 Destination는 해당 Branch Code 사용)
                s_qbuff = "SELECT bi_cd, bi_ename, bi_hostname, " +
                                    "CAST(DecryptByKey(bi_dbhost) AS varchar(100)) AS dbhost, " +
                                    "CAST(DecryptByKey(bi_dbport) AS varchar(100)) AS dbport, " +
                                    "CAST(DecryptByKey(bi_dbuserid) AS varchar(100)) AS dbuserid, " +
                                    "CAST(DecryptByKey(bi_dbpassword) AS varchar(100)) AS dbpassword, " +
                                    "CAST(DecryptByKey(bi_dbname) AS varchar(100)) AS dbname " +
                            "FROM tb_branchinfo " +
                            "WHERE 1 = 1 " + (c_covar.host_type == "C" ? "AND bi_hostname = '" + c_covar.host_remotename + "' " : "") +
                                "AND bi_hosttype IN (" + (c_covar.host_type == "C" ? "'S','R'" : "'C'") + ") " +
                                "AND bi_active = '1' " +
                        "ORDER BY bi_cd";

                //c_colib.cWriteLogs(c_sProcessor, "Query: " + s_qbuff + " (" + c_sMethod + ")!!");

                if (c_localdb.RsOpen(s_qbuff) >= 0 && c_localdb.rs.RecordCount > 0)
                {
                    int j, iBranchCount = c_localdb.rs.RecordCount;

                    c_remotedb = new cls_codb00[iBranchCount];
                    c_sBranch = new string[iBranchCount];

                    j = 0;

                    while (!c_localdb.rs.EOF)
                    {
                        c_remotedb[j] = new cls_codb00();

                        c_sBranch[j] = Convert.ToString(c_localdb.rs.Fields["bi_cd"].Value);

                        c_remotedb[j].db_host = Convert.ToString(c_localdb.rs.Fields["dbhost"].Value);
                        c_remotedb[j].db_port = Convert.ToString(c_localdb.rs.Fields["dbport"].Value);
                        c_remotedb[j].db_name = Convert.ToString(c_localdb.rs.Fields["dbname"].Value);
                        c_remotedb[j].db_userid = Convert.ToString(c_localdb.rs.Fields["dbuserid"].Value);
                        c_remotedb[j].db_password = Convert.ToString(c_localdb.rs.Fields["dbpassword"].Value);

                        c_localdb.rs.MoveNext();
                        j++;
                    }

                    c_localdb.CloseSymmetricKey();
                    c_localdb.RsClose();

                    _Semaphore = new SemaphoreSlim(iBranchCount);

                    for (int index = 1; index <= iBranchCount; index++)
                    {
                        new Thread(DoProcessingOuterData).Start(index);

                        Thread.Sleep(500);
                    }
                }
                else
                {
                    c_localdb.CloseSymmetricKey();
                    c_localdb.RsClose();

                    c_colib.cWriteLogs(c_sProcessor, "Failed to verify remote host identification. The information does not match on the database [" + c_localdb.error_message.ToString() + "] (" + c_sMethod + ")");

                    //return;
                }
            }
            catch (SqlException ex)
            {
                c_colib.cWriteLogs(c_sProcessor, "Query Error [" + ex.Message.ToString() + "] (" + c_sMethod + ")");
            }
            catch (Exception e)
            {
                c_colib.cWriteLogs(c_sProcessor, "Error [" + e.Message.ToString() + "] (" + c_sMethod + ")");
            }
            finally
            {
                //_Semaphore.Release();
            }
        }

        static void Destructor(object sender, EventArgs e)
        {
            c_covar = null;

            if (c_localdb != null) if (c_localdb.rs != null) c_localdb.CloseSymmetricKey();

            if(c_localdb != null) c_localdb.DBClose();

            if (c_remotedb != null)
            {
                for (int i = 0; i < c_remotedb.GetUpperBound(0); i++)
                {
                    if (c_remotedb[i] != null) c_remotedb[i].DBClose();
                }
            }

            c_localdb = null;
            c_remotedb = null;
            _Semaphore = null;
        }

        private static void DoProcessingOuterData(object id)
        {
            string s_qbuff = string.Empty;
            string s_error = string.Empty;
            Recordset rs_dqrecordset = new Recordset();

            int iIndex = 0;
            long lErrorCode = 0;

            rs_dqrecordset = new Recordset();

            iIndex = Convert.ToInt32(id) - 1;

            //c_colib.cWriteLogs(c_sProcessor, "Procedure started (" + c_sMethod + ")");

            if (c_localdb.DBConnection() < 1)
            {
                c_colib.cWriteLogs(c_sProcessor, "Local database connection failed (" + c_sMethod + ")!!");
                return;
            }

            s_qbuff = "SELECT dq_seq, dq_trandate, dq_trantime, dq_source, dq_destination, dq_dbname, dq_key, dq_dmltype, dq_dbquery, dq_target_range, dq_datetime, dq_trigger, dq_error " +
                        "FROM tb_syncdataque " +
                       "WHERE dq_destination = '" + c_sBranch[iIndex] + "' " +
                       "ORDER BY dq_trandate, dq_trantime";

            //c_colib.cWriteLogs(c_sProcessor, "QueData Query sent [" + s_qbuff + "] (" + c_sMethod + ")");

            _Semaphore.Wait();

            if (c_localdb.RsOpen(ref rs_dqrecordset, s_qbuff) >= 0 && rs_dqrecordset.RecordCount > 0)
            {
                try
                {
                    if (c_remotedb[iIndex].DBConnection() < 1)
                    {
                        c_colib.cWriteLogs(c_sProcessor, "Remote database connection failed [" + c_sBranch[iIndex] + "] (" + c_sMethod + ") " + c_remotedb[iIndex].db_host);
                        _Semaphore.Release();

                        return;
                    }

                    //c_colib.cWriteLogs(c_sProcessor, "Remote database connection [" + c_sBranch[iIndex] + "] (" + c_sMethod + ") " + c_remotedb[iIndex].db_name + "|" + c_remotedb[iIndex].db_params);

                    while (!rs_dqrecordset.EOF)
                    {
                        s_qbuff = "SELECT TOP 1 dq_trandate, dq_trantime, dq_source, dq_destination, dq_dbname, dq_key, dq_dmltype, dq_dbquery, dq_target_range, dq_datetime, dq_trigger " +
                                    "FROM tb_syncdataque " +
                                   "WHERE dq_trandate = '1'";

                        try
                        {
                            if (c_remotedb[iIndex].RsOpen(s_qbuff) >= 0 && c_remotedb[iIndex].rs.RecordCount == 0)
                            {
                                c_remotedb[iIndex].rs.AddNew();

                                c_remotedb[iIndex].rs.Fields["dq_trandate"].Value = rs_dqrecordset.Fields["dq_trandate"].Value;
                                c_remotedb[iIndex].rs.Fields["dq_trantime"].Value = rs_dqrecordset.Fields["dq_trantime"].Value;
                                c_remotedb[iIndex].rs.Fields["dq_source"].Value = rs_dqrecordset.Fields["dq_source"].Value;
                                c_remotedb[iIndex].rs.Fields["dq_destination"].Value = rs_dqrecordset.Fields["dq_destination"].Value;
                                c_remotedb[iIndex].rs.Fields["dq_dbname"].Value = rs_dqrecordset.Fields["dq_dbname"].Value;
                                c_remotedb[iIndex].rs.Fields["dq_key"].Value = rs_dqrecordset.Fields["dq_key"].Value;
                                c_remotedb[iIndex].rs.Fields["dq_dmltype"].Value = rs_dqrecordset.Fields["dq_dmltype"].Value;
                                c_remotedb[iIndex].rs.Fields["dq_dbquery"].Value = rs_dqrecordset.Fields["dq_dbquery"].Value;
                                //sSyncDataQue.dq_dbquery = sSyncDataQue.dq_dbquery.Replace("'", "''");
                                c_remotedb[iIndex].rs.Fields["dq_target_range"].Value = rs_dqrecordset.Fields["dq_target_range"].Value;
                                c_remotedb[iIndex].rs.Fields["dq_datetime"].Value = rs_dqrecordset.Fields["dq_datetime"].Value;
                                c_remotedb[iIndex].rs.Fields["dq_trigger"].Value = rs_dqrecordset.Fields["dq_trigger"].Value;

                                c_remotedb[iIndex].rs.Update();
                                c_remotedb[iIndex].rs.Close();

                                Thread.Sleep(100);

                                rs_dqrecordset.Delete();

                                Thread.Sleep(100);

                                //c_colib.cWriteLogs(c_sProcessor, "Local data deleted (" + c_sMethod + ")");
                            }
                        }
                        catch (SqlException sqlex)
                        {
                            c_colib.cWriteLogs(c_sProcessor, "Remote QueData not applied ID[" + iIndex.ToString() + ", " + c_sBranch[iIndex] + "], HOST[" + c_remotedb[iIndex].db_host + "] (" + c_sMethod + ")");

                            if (lErrorCode != 0)
                            {
                                //c_colib.cWriteLogs(c_sProcessor, "Remote query [" + s_qbuff + "] (" + c_sMethod + ")");
                                c_colib.cWriteLogs(c_sProcessor, "Remote query Error [" + sqlex.Message.ToString() + "] (" + c_sMethod + ")");

                                rs_dqrecordset.Fields["dq_error"].Value = c_remotedb[iIndex].error_message;
                                rs_dqrecordset.Update();
                            }
                        }

                        rs_dqrecordset.MoveNext();
                    }

                    c_localdb.RsClose(ref rs_dqrecordset);

                    rs_dqrecordset = null;
                }
                catch (SqlException ex)
                {
                    s_error = "Line:" + ex.LineNumber.ToString() + ", " + ex.Message.ToString();
                    c_colib.cWriteLogs(c_sProcessor, "Query Error [" + s_error + "] (" + c_sMethod + ")!!");
                }
                catch (Exception e)
                {
                    s_error = "Line:" + e.Source.ToString() + ", " + e.Message.ToString();
                    c_colib.cWriteLogs(c_sProcessor, "Query Error [" + s_error + "] (" + c_sMethod + ")!!");
                }
                finally
                {
                    c_remotedb[iIndex] = null;

                    Thread.Sleep(500);

                    _Semaphore.Release();
                }
            }

            //Thread.Sleep(1000);

            //_Semaphore.Release();
        }

        public static string GetCurrentMethodName()
        {
            var st = new StackTrace(new StackFrame(1));
            return st.GetFrame(0).GetMethod().Name;
        }
    }
}
