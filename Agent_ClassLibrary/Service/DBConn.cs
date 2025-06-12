using Agent_ClassLibrary.Gloab;
using Microsoft.Data.SqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Agent_ClassLibrary.Service
{
    public class DBConn :IDBConn
    {   
        


        private readonly int CommandTimeOut = 0;
        private ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();
        public DataSet SqlQuery(string strDB, string strSql, Hashtable prm, string guid, string type)
        {
            DataSet dataSet = new DataSet();
            string connectionString = ConfigurationManager.ConnectionStrings[strDB].ConnectionString;
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                using (SqlCommand sqlCommand = new SqlCommand())
                using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter())
                {
                    sqlCommand.Connection = sqlConnection;
                    sqlCommand.CommandText = strSql;
                    // 執行命令完畢的等待時間 0 為永遠等待
                    sqlCommand.CommandTimeout = CommandTimeOut;

                    foreach (DictionaryEntry item in prm)
                    {
                        string prmName = item.Key.ToString();
                        string prmType = item.Value.GetType().Name.ToUpper();
                        SqlParameter sqlParameter;

                        switch (prmType)
                        {
                            case "BYTE[]":
                                sqlParameter = new SqlParameter(prmName, SqlDbType.VarBinary);
                                break;
                            case "DATATIME":
                                sqlParameter = new SqlParameter(prmName, SqlDbType.DateTime);
                                break;
                            default:
                                sqlParameter = new SqlParameter(prmName, SqlDbType.VarChar);
                                break;
                        }

                        sqlParameter.Value = item.Value;

                        sqlCommand.Parameters.Add(sqlParameter);
                    }
                    sqlConnection.Open();
                    sqlDataAdapter.SelectCommand = sqlCommand;
                    sqlDataAdapter.Fill(dataSet);

                }
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message.ToString(), guid, type);
                throw ex;
            }

            return dataSet;
        }

        public bool SqlUpdate(string strDB, string strSql, Hashtable prm, ref int counts, string guid, string type)
        {
            bool result = false;
            string connectionString = ConfigurationManager.ConnectionStrings[strDB].ConnectionString;
            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            using (SqlCommand sqlCommand = new SqlCommand())
            using (SqlDataAdapter dataAdapter = new SqlDataAdapter())
            {

                sqlCommand.CommandText = strSql;
                sqlCommand.Connection = sqlConnection;
                // 執行命令完畢的等待時間 0 為永遠等待
                sqlCommand.CommandTimeout = CommandTimeOut;
                try
                {
                    foreach (DictionaryEntry item in prm)
                    {
                        string parameterName = item.Key.ToString();
                        string prmType = item.Value.GetType().Name.ToUpper();
                        SqlParameter sqlParameter;
                        switch (prmType)
                        {
                            case "BYTE[]":
                                sqlParameter = new SqlParameter(parameterName, SqlDbType.VarBinary);
                                break;
                            case "DATETIME":
                                sqlParameter = new SqlParameter(parameterName, SqlDbType.DateTime);
                                break;
                            default:
                                sqlParameter = new SqlParameter(parameterName, SqlDbType.VarChar);
                                break;
                        }

                        sqlParameter.Value = item.Value;
                        sqlCommand.Parameters.Add(sqlParameter);
                    }

                    sqlConnection.Open();
                    // 并返回受影响的行数。它返回一个整数，表示已经执行的 SQL 命令影响到的行数。
                    counts += sqlCommand.ExecuteNonQuery();
                    result = true;
                }
                catch (Exception ex)
                {
                    result = false;
                    //_commonQueryLCU.LogToFile(ex.Message);
                    WriteLog(ex.Message.ToString(), guid, type);
                    throw ex;
                }


                return result;

            }
        }

        /// <summary>
        /// 執行預存程序
        /// </summary>
        /// <param name="strDB">資料庫名稱</param>
        /// <param name="SpName">SP名稱</param>
        /// <param name="Prm">參數</param>
        /// <param name="OutPrm">OutPut參數</param>
        /// <returns></returns>
        public DataSet SqlSp(string strDB, string SpName, Hashtable Prm, string guid, string type)
        {
            DataSet dataSet = new DataSet();
            string connectionString = ConfigurationManager.ConnectionStrings[strDB].ConnectionString;

            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                using (SqlCommand sqlCommand = new SqlCommand(SpName, sqlConnection))
                using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter())
                {
                    ArrayList arrKey = new ArrayList();
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.CommandTimeout = 3600;
                    foreach (DictionaryEntry entry in Prm)
                    {
                        string strKey = entry.Key.ToString();
                        sqlCommand.Parameters.Add(strKey, SqlDbType.VarChar);
                        sqlCommand.Parameters[strKey].Value = entry.Value;
                    }
                    sqlCommand.Connection.Open();
                    SqlDataAdapter dapter = new SqlDataAdapter(sqlCommand);
                    dapter.Fill(dataSet);
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message.ToString(), guid, type);
                throw;
            }

            return dataSet;
        }

        public async Task<DataSet> asyncSqlSp(string strDB, string SpName, Hashtable Prm, string guid, string type)
        {
            DataSet dataSet = new DataSet();
            string connectionString = ConfigurationManager.ConnectionStrings[strDB].ConnectionString;

            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                using (SqlCommand sqlCommand = new SqlCommand(SpName, sqlConnection))
                using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter())
                {
                    ArrayList arrKey = new ArrayList();
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.CommandTimeout = 3600;
                    foreach (DictionaryEntry entry in Prm)
                    {
                        string strKey = entry.Key.ToString();
                        sqlCommand.Parameters.Add(strKey, SqlDbType.VarChar);
                        sqlCommand.Parameters[strKey].Value = entry.Value;
                    }
                   await  sqlCommand.Connection.OpenAsync();
                    SqlDataAdapter dapter = new SqlDataAdapter(sqlCommand);
                    //dapter.Fill(dataSet);
                    await Task.Run(() => dapter.Fill(dataSet));
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message.ToString(), guid, type);
            }

            return dataSet;
        }

        public async Task<bool> InsertWithSqlBulkCopy(string strDB, string TableName, DataTable dataTable, string guid, string type)
        {
            bool result = false;
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings[strDB].ConnectionString;

                await using SqlConnection sqlConnection = new SqlConnection(connectionString);
                await sqlConnection.OpenAsync();

                using SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(sqlConnection)
                {
                    DestinationTableName = TableName,
                };

                await sqlBulkCopy.WriteToServerAsync(dataTable);
                result= true;
            }
            catch (Exception ex)
            {

                result = false;
                //_commonQueryLCU.LogToFile(ex.Message);
                WriteLog(ex.Message.ToString(), guid, type);
                throw ex;
            }
            return result;
        }


        /// <summary>
        /// 寫Log，並建立筆記本Log
        /// </summary>
        /// <param name="msg"></param>
        #region 寫Log，並建立筆記本Log
        public void WriteLog(string msg, string guid, string type)
        {
            //msg = DateTime.Now.ToString("HH:mm:ss")
            //       + "^" + type
            //       + "^" + msg;

            string txtMsg = $"{DateTime.Now.ToString("HH:mm:ss")} 步驟 : {type} GUID : {guid} 訊息 : {msg}";

            //PrintLog
            Console.WriteLine(txtMsg);

            string FilePath = AppDomain.CurrentDomain.BaseDirectory + @"Log\" + guid + "_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
            Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + @"Log");


            //這樣可以確保在該執行緒執行寫入操作期間，其他執行緒無法進行寫入或讀取操作。
            _readWriteLock.EnterWriteLock();

            //FileMode.Append 表示如果檔案存在，會將資料附加到檔案的末尾，如果檔案不存在，則會建立新的檔案。
            //FileAccess.Write 表示可以對檔案進行寫入操作。
            //FileShare.ReadWrite 表示允許多個讀取和寫入操作。
            using (FileStream fs = new FileStream(FilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.Default, 4096))
                {
                    sw.WriteLine(txtMsg);
                    sw.Close();
                }
            }
            _readWriteLock.ExitWriteLock();
        }
        #endregion

    }
}
