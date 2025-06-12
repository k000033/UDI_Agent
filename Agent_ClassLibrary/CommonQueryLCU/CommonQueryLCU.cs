using Agent_ClassLibrary.Ftp;
using Agent_ClassLibrary.Gloab;
using Agent_ClassLibrary.Service;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agent_ClassLibrary.CommonQueryLCU
{
    public class CommonQueryLCU :ICommonQueryLCU
    {
        private readonly IDBConn _dBConn;
        private readonly IGlobalUtility _global;
        public CommonQueryLCU(IDBConn dBConn, IGlobalUtility global)
        {
            _dBConn = dBConn;
            _global = global;
        }

        private string _Parameter_Url;
        private string _Parameter_GUID;
        private string _Parameter_OrderType;
        private string _Parameter_Step;
        private string _Parameter_OrderId;
        private string _Parameter_Value;
        private ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();
        public string Parameter_Url { get => _Parameter_Url; set => _Parameter_Url = value; }
        public string Parameter_GUID { get => _Parameter_GUID; set => _Parameter_GUID = value; }
        public string Parameter_OrderType { get => _Parameter_OrderType; set => _Parameter_OrderType = value; }
        public string Parameter_Step { get => _Parameter_Step; set => _Parameter_Step = value; }

        public string Parameter_OrderId { get => _Parameter_OrderId; set => _Parameter_OrderId = value; }
        public string Parameter_Value { get => _Parameter_Value; set => _Parameter_Value = value; }
        /// <summary>
        /// 寫Log，並建立筆記本Log
        /// </summary>
        /// <param name="msg"></param>
        #region 寫Log，並建立筆記本Log
        public void Agent_WriteLog(string msg)
        {
            msg = DateTime.Now.ToString("HH:mm:ss")
                   + "^" + Parameter_Step
                   + "^" + msg;

            //PrintLog
            Console.WriteLine(msg);

            string FilePath = AppDomain.CurrentDomain.BaseDirectory + @"Log\" + Parameter_GUID + "_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
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
                    sw.WriteLine(msg);
                    sw.Close();
                }
            }
            _readWriteLock.ExitWriteLock();
        }
        #endregion


        /// <summary>
        /// FTP 連線
        /// </summary>
        /// <param name="ftpClient"></param>
        /// <exception cref="NotImplementedException"></exception>
        #region FTP 連線
        public void FTP_Connectoin(ref FtpClient ftpclient)
        {
            // 連結ftp 
            ftpclient = new FtpClient(@"ftp://" + Parameter_Url + @":21/", "anonymous", "anonymous");

            //測試連線是否正常
            string[] files = ftpclient.directoryListSimple(@"/");

            Agent_WriteLog(" PC<->" + Parameter_Url);
            //string FileDirectory = AppDomain.CurrentDomain.BaseDirectory + Parameter_Divice_AREA + @"\" + Parameter_Divice_ID;
            //Directory.CreateDirectory(FileDirectory);
        }
        #endregion




        /// <summary>
        ///斷文件是否存在
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="ftp"></param>
        /// <param name="ExistType">0:檔案要存在 1:檔案要不存在</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        #region 判斷文件是否存在
        public string FTPCheckFileUploadOK(string FileName, ref FtpClient ftp, int ExistType)
        {
            bool IsOK = false;
            int CheckSecond = 0;
            int MaxSeconds = 30;
            string[] files;

            //第一關：檢查檔案存在
            //如果是要檢查檔案消失的設定, 有可能檔案下傳後馬上消失, 來不及先進入第一關的確認檔案存在
            try
            {
                IsOK = false;

                while (true)
                {
                    //CheckFileSize = ftp.getFileSize(FileName);
                    //CheckSecond = MaxWaitSecond + 1;
                    //Steps = 1;

                    files = ftp.directoryListSimple("");
                    // 找到文件，結束迴圈
                    foreach (string file in files)
                    {
                        if (file == FileName)
                        {
                            IsOK = true;
                            break;
                        }
                    }

                    if (IsOK)
                        break;

                    // 如果是 檢查檔案消失 ，先檢查檔案是否存在，超過時間 離開
                    if (ExistType == 1)
                    {
                        if (CheckSecond >= MaxSeconds)
                        {
                            Agent_WriteLog($" 等待產生 {FileName}，並等待檔案消失超過30秒");
                            break;
                        }
  
                    }
                    Thread.Sleep(1000);
                    CheckSecond += 1;
                    Console.CursorLeft = 0;
                    Agent_WriteLog($" 等待產生 {FileName}，{CheckSecond} ...");
                }
                Console.WriteLine("");

                //第二關：檔案要消失, 才能離開
                if (ExistType == 1)
                {
                    while (true)
                    {
                        IsOK = true;
                        files = ftp.directoryListSimple("");

                        foreach (string file in files)
                        {
                            if (file == FileName)
                                IsOK = false;
                        }

                        if (IsOK)
                            break;

                        Thread.Sleep(1000);
                        CheckSecond += 1;
                        Console.CursorLeft = 0;
                        Agent_WriteLog($" 等待 {FileName} 消失 {CheckSecond} ...");
                    }
                    Console.WriteLine("");
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            #region 檔案下傳時間寫入log
            string strlog = " 下傳 " + FileName;
            if (ExistType == 1)
                strlog += " 並等待消失.";
            strlog += " 共花費:" + CheckSecond.ToString() + " 秒";
            Agent_WriteLog(strlog);
            #endregion

            Console.WriteLine("");
            return "";
        }
        #endregion



        /// <summary>
        /// 將 LCU0301 文件 寫入 TABLE
        /// </summary>
        /// <param name="prm"></param>  參數
        /// <param name="intItems"></param>  行數
        /// <returns></returns>
        #region 將 LCU0301 文件 寫入 TABLE
        public bool TXTTODB_LCU0301(Hashtable prm, ref int intItems)
        {
            string strSql = @"INSERT [ftp].[UDI_GET] (GUID,IDX,TXT,CRT_TIME)
                              SELECT @GUID
                                    ,@IDX
	                                ,@TXT
	                                ,GETDATE()";
            bool result = _dBConn.SqlUpdate("UDI", strSql, prm, ref intItems, Parameter_GUID, Parameter_Step);
            return result;
        }
        #endregion


        /// <summary>
        /// LCU:取得TXT
        /// </summary>
        /// <param name="DEVICE_AREA"></param>
        /// <param name="DEVICE_ID"></param>
        /// <returns></returns>
        #region LCU:取得TXT
        public DataTable GetTxtFromLCU(Hashtable prm)
        {
            //string strSp = @"[dbo].[spUDI_EXECUTE]";

            //DataSet dt = _dBConn.SqlSp("UDI", strSp, prm, Parameter_GUID, Parameter_Step);
            //return dt;

            DataTable dataTable = new DataTable();
            try
            {
                string strSql = @"SELECT TXT
                                FROM [ftp].[UDI_PUT]
                               WHERE GUID = @GUID
                               ORDER BY IDX ";

                dataTable = _dBConn.SqlQuery("UDI", strSql, prm, _global.Parameter_GUID, _global.Parameter_Step).Tables[0];

            }
            catch (Exception ex)
            {

                _global.LogToDatabase(ex.Message);
            }
            return dataTable;
        }
        #endregion


        public DataSet WhriteToResult()
        {
            string strSp = @"[dbo].[spUDI_EXECUTE]";
            Hashtable ht_Query = new Hashtable();
            ht_Query.Add("GUID", Parameter_GUID);
            DataSet dt = _dBConn.SqlSp("UDI", strSp, ht_Query, Parameter_GUID, Parameter_Step);
            return dt;
        }


        public DataSet UpdateState()
        {
            string strSp = @"[dbo].[spUDI_STATE]";
            Hashtable ht_Query = new Hashtable();
            ht_Query.Add("GUID", Parameter_GUID);
            DataSet dataSet = _dBConn.SqlSp("UDI", strSp, ht_Query, Parameter_GUID, Parameter_Step);
            return dataSet;
        }
  

        // 清除
        public void Agent_Clean()
        {
            string FilePathLog = AppDomain.CurrentDomain.BaseDirectory + "\\log";
            string FilePath = AppDomain.CurrentDomain.BaseDirectory + Parameter_Url +  Parameter_GUID;
            string FileDirectory_SendBackup = FilePath + "\\SendDataBackup";
            string FileDirectory_ReturnBackup = FilePath + "\\RcvDataBackup";


            //清除 agent本身的log
            string[] directorys = Directory.GetDirectories(FilePath);
            foreach (string directory in directorys)
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(directory);
                if( directoryInfo.LastWriteTime<= DateTime.Now.AddDays(-7))
                {
                    Directory.Delete(directory, true);
                }

            }
        }

    }
}
