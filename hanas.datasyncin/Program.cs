namespace hanas.datasynccontroller
{
    using System;
    using System.Threading;
    using System.Data;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Reflection;
    using ADODB;

    using hanas.com.covar;
    using hanas.com.colibs;
    using hanas.com.codb;


    internal partial class Program
    {
        private static SemaphoreSlim _Semaphore;

        private static cls_covar00 c_covar = new cls_covar00();
        private static cls_colibs00 c_colib = new cls_colibs00();
        private static cls_codb00 c_localdb;

        private static string c_sProcessor = System.AppDomain.CurrentDomain.FriendlyName.Replace(".exe", "");
        private static string c_sMethod;

        private static Object obj = new Object();


        private static void Main(string[] args)
        {
            int i = 0;
            string s_qbuff = string.Empty, s_error = string.Empty;

            c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            if (args.Length == 3)
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

                //Getting Local Server Information
                s_qbuff = "SELECT bi_ename " +
                            "FROM tb_branchinfo " +
                           "WHERE bi_cd = '" + c_covar.host_localcode + "' " +
                             "AND bi_hosttype = '" + c_covar.host_type + "' " +
                             "AND bi_hostname = '" + c_covar.host_localname + "' " +
                             "AND bi_active = '1'";

                if (c_localdb.RsOpen(s_qbuff) >= 0 && c_localdb.rs.RecordCount > 0)
                {
                    //c_colib.cWriteLogs(c_sProcessor, "Query data retrieved (" + c_sMethod + ")!!");

                    c_localdb.RsClose();

                    _Semaphore = new SemaphoreSlim(1);

                    DoProcessingInnerData(c_covar.host_localcode);
                }
                else
                {
                    c_colib.cWriteLogs(c_sProcessor, "Failed to verify local host identification (" + c_sMethod + ")!!");

                    return;
                }
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
                c_localdb.DBClose();
                c_localdb = null;
            }
        }

        private static void DoProcessingInnerData(string localcode)
        {
            c_sMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            string s_xbuff = string.Empty;
            string s_qbuff = string.Empty;
            string s_error = string.Empty;
            string s_dqquery = string.Empty;

            Recordset rs_dqrecordset = new Recordset();

            long lErrorCode = 0;

            _Semaphore.Wait();

            //c_colib.cWriteLogs(c_sProcessor, "Procedure started (" + localcode + ") (" + c_sMethod + ")");

            if (c_localdb.DBConnection() < 1)
            {
                c_colib.cWriteLogs(c_sProcessor, "Database connection failed (" + c_sMethod + ")!!");
                return;
            }
            else
            {
                //c_colib.cWriteLogs(c_sProcessor, "DBConnection success!! (" + localcode + ") (" + c_sMethod + ")");
            }

            s_xbuff = "SELECT dq_seq, dq_source, dq_key, dq_dmltype, dq_dbname, dq_dbquery, dq_target_range, dq_error " +
                        "FROM tb_syncdataque " +
                        "WHERE dq_destination = '" + localcode + "' " +
                            "AND dq_target_range = '1' " +
                            "AND dq_error IS NULL " +
                        "ORDER BY dq_trandate, dq_trantime";

            //c_colib.cWriteLogs(c_sProcessor, "Query: " + s_xbuff + " (" + c_sMethod + ")!!");

            try
            {
                if (c_localdb.RsOpen(ref rs_dqrecordset, s_xbuff) >= 0 && rs_dqrecordset.RecordCount > 0)
                {
                    while (!rs_dqrecordset.EOF)
                    {
                        s_error = "";

                        s_dqquery = string.Format(rs_dqrecordset.Fields["dq_dbquery"].Value.ToString(), rs_dqrecordset.Fields["dq_dbname"].Value);
                        s_qbuff = c_covar.host_type == "S" ? s_dqquery : s_dqquery.Replace("''", "'");  // Distributor의 경우, 전달해 준 서버에서 동일 쿼리를 실행하기 위해서 아포스트로피 그대로 유지

                        lErrorCode = c_localdb.DBExcute(s_qbuff);

                        Thread.Sleep(100);

                        if (c_localdb.affectedrecords != 0)
                        {
                            rs_dqrecordset.Delete();

                            Thread.Sleep(100);
                        }
                        else
                        {
                            if (lErrorCode == 0)
                            {
                                lErrorCode = c_localdb.DBExcute("INSERT INTO HanaSystem.dbo.tb_syncdataque_notfound (dq_trandate, dq_trantime, dq_source, dq_destination, dq_dbname, dq_key, dq_dmltype, dq_dbquery, dq_target_range, dq_datetime, dq_trigger, dq_error) " +
                                                                 "SELECT dq_trandate, dq_trantime, dq_source, dq_destination, dq_dbname, dq_key, dq_dmltype, dq_dbquery, dq_target_range, dq_datetime, dq_trigger, dq_error " +
                                                                   "FROM tb_syncdataque " +
                                                                  "WHERE dq_seq = " + rs_dqrecordset.Fields["dq_seq"].Value.ToString() + " " +
                                                                    "AND dq_destination = '" + localcode + "' " +
                                                                    "AND dq_target_range = '1' " +
                                                                    "AND dq_error IS NULL");

                                if (lErrorCode == 0)
                                {
                                    rs_dqrecordset.Delete();

                                    Thread.Sleep(100);
                                }
                                else
                                    c_colib.cWriteLogs(c_sProcessor, "Query not affected [" + s_qbuff + "] (" + c_sMethod + ")!!");
                            }
                            else
                            {
                                //c_colib.cWriteLogs(c_sProcessor, "Query [" + s_qbuff + "] (" + c_sMethod + ")!!");
                                //c_colib.cWriteLogs(c_sProcessor, "Query Error [" + c_localdb.error_message + "] (" + c_sMethod + ")!!");

                                rs_dqrecordset.Fields["dq_error"].Value = c_localdb.error_message;
                                rs_dqrecordset.Update();

                                Thread.Sleep(100);
                            }
                        }

                        rs_dqrecordset.MoveNext();
                    }       // while

                    c_localdb.RsClose(ref rs_dqrecordset);
                }
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
                c_localdb.DBClose();

                rs_dqrecordset = null;
                c_localdb = null;

                Thread.Sleep(500);

                _Semaphore.Release();
            }
            //}
        }
    }
}
