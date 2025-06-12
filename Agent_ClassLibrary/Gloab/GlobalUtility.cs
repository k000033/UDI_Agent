using Agent_ClassLibrary.Ftp;
using Agent_ClassLibrary.Service;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Agent_ClassLibrary.Gloab
{
    public class GlobalUtility : IGlobalUtility
    {

        private readonly IDBConn _dBConn;
        public GlobalUtility(IDBConn dBConn)
        {
            _dBConn = dBConn;
        }

        public GlobalUtility() { }


        private string _Parameter_Url;
        private string _Parameter_GUID;
        private string _Parameter_OrderType;
        private string _Parameter_Step;
        private string _Parameter_OrderId;
        private string _Parameter_TaskId;
        private string _Parameter_DeviceId;
        private ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();
        public string Parameter_Url { get => _Parameter_Url; set => _Parameter_Url = value; }
        public string Parameter_GUID { get => _Parameter_GUID; set => _Parameter_GUID = value; }
        public string Parameter_OrderType { get => _Parameter_OrderType; set => _Parameter_OrderType = value; }
        public string Parameter_Step { get => _Parameter_Step; set => _Parameter_Step = value; }

        public string Parameter_OrderId { get => _Parameter_OrderId; set => _Parameter_OrderId = value; }
        public string Parameter_TaskId { get => _Parameter_TaskId; set => _Parameter_TaskId = value; }
        public string Parameter_DeviceId { get => _Parameter_DeviceId; set => _Parameter_DeviceId = value; }
        /// <summary>
        /// 寫Log，並建立筆記本Log
        /// </summary>
        /// <param name="msg"></param>
        #region 寫Log，並建立筆記本Log
        public void LogToFile(string msg)
        {

            // 時間 藍色
            //Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("{0,-10}", DateTime.Now.ToString("HH:mm:ss"));
            // 步驟 綠色
            //Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("{0,-5}", Parameter_Step);
            // 重製顏色
            //Console.ResetColor();
            // 訊息 白字
            Console.Write(msg);
            // 換行
            Console.WriteLine();


            //msg = DateTime.Now.ToString("HH:mm:ss")
            //         + "^" + Parameter_Step
            //         + "^" + msg;

            //Console.WriteLine(msg);



            string FilePath = AppDomain.CurrentDomain.BaseDirectory + @"Log\" + Parameter_TaskId + "_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
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
                    string space = new string(' ', 5);
                    string txtMsg = $"{DateTime.Now.ToString("HH:mm:ss")} 步驟 : {Parameter_Step} GUID : {Parameter_GUID} 訊息 : {msg}";
                    sw.WriteLine(txtMsg);
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

            try
            {
                // 連結ftp 
                ftpclient = new FtpClient(@"ftp://" + Parameter_Url + @":21/", "anonymous", "anonymous");

                //測試連線是否正常
                string[] files = ftpclient.directoryListSimple(@"/");

                LogToFile(" PC<->" + Parameter_Url);
                //string FileDirectory = AppDomain.CurrentDomain.BaseDirectory + Parameter_Divice_AREA + @"\" + Parameter_Divice_ID;
                //Directory.CreateDirectory(FileDirectory);
            }
            catch (Exception ex)
            {
                LogToFile($"{Parameter_Url} ftp 連線失敗");
                LogToDatabase($"{Parameter_Url} ftp 連線失敗");
                LineNotice($"{Parameter_Url} ftp 連線失敗");
            }

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
            int MaxSeconds = 1800;
            string[] files;

            //第一關：檢查檔案存在
            //如果是要檢查檔案消失的設定, 有可能檔案下傳後馬上消失, 來不及先進入第一關的確認檔案存在
            try
            {

                if (ExistType == 0)
                {
                    IsOK = false;
                    while (true)
                    {


                        //LogToFile($"第一關：檢查檔案存在 {CheckSecond}");

                        files = ftp.directoryListSimple("");

                        if (files == null)
                        {
                            throw new Exception("FTP 檔案列表為空");
                        }

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
                        {
                            break;

                        }


                        if (CheckSecond >= MaxSeconds)
                        {
                            LineNotice($"等待產生 {FileName}，並等待檔案消失超過{MaxSeconds}秒");
                            LogToFile($"等待產生 {FileName}，並等待檔案消失超過{MaxSeconds}秒");
                            LogToDatabase($"等待產生 {FileName}，並等待檔案消失超過{MaxSeconds}秒");
                            break;
                        };


                        Thread.Sleep(1000);
                        CheckSecond += 1;


                        // 嘗試寫入 Log
                        try
                        {
                            LogToFile($" 等待產生 {FileName}，{CheckSecond} ...");
                        }
                        catch (Exception logEx)
                        {
                            LogToFile($" 無法寫入日誌: {logEx.Message}");
                        }
                    }
                }



                //第二關：檔案要消失, 才能離開
                if (ExistType == 1)
                {

                    // 定義匹配 fileName.* 的正則表達式
                    string filePattern = $"^{FileName}\\..*$";
                    var regex = new System.Text.RegularExpressions.Regex(filePattern);


                    CheckSecond = 0;
                    while (true)
                    {
                        IsOK = true;
                        files = ftp.directoryListSimple("");

                        foreach (string file in files)
                        {
                            if (regex.IsMatch(file))
                                IsOK = false;
                        }

                        if (IsOK)
                            break;

                        Thread.Sleep(1000);
                        CheckSecond += 1;


                        if (CheckSecond >= MaxSeconds)
                        {
                            LogToFile($"等待{FileName},檔案消失超過 {MaxSeconds}秒");
                            LogToDatabase($"等待{FileName},檔案消失超過{MaxSeconds}秒");
                            break;
                        };



                        Console.CursorLeft = 0;
                        LogToFile($" 等待 {FileName} 消失 {CheckSecond} ...");
                    }
                    Console.WriteLine("");
                }
            }
            catch (Exception ex)
            {
                LogToDatabase(ex.Message);
                return ex.Message;

            }


            #region 檔案下傳時間寫入log
            LogToFile($"產生 {FileName}");
            string strlog = "產生 " + FileName;

            if (ExistType == 1)
            {
                strlog += " 並等待消失.";

            }

            strlog += " 共花費:" + CheckSecond.ToString() + " 秒";
            LogToFile(strlog);
            #endregion
            return "";
        }
        #endregion

        // 異動狀態

        public async Task<DataSet> UpdateState()
        {
            DataSet dataSet = null;
            try
            {
                string strSp = @"[dbo].[spUDI_STATE]";
                Hashtable ht_Query = new Hashtable();
                ht_Query.Add("GUID", Parameter_GUID);
                dataSet = await Task.Run(() => _dBConn.SqlSp("UDI", strSp, ht_Query, Parameter_TaskId, Parameter_Step));
            
            }
            catch (Exception ex)
            {
                LogToDatabase(ex.Message);
            }

            return dataSet;
        }

        public void UpdateState1()
        {
            try
            {
                string strSp = @"[dbo].[spUDI_STATE]";
                Hashtable ht_Query = new Hashtable();
                ht_Query.Add("GUID", Parameter_GUID);
                _dBConn.SqlSp("UDI", strSp, ht_Query, Parameter_TaskId, Parameter_Step);
            }
            catch (Exception ex)
            {
                LogToDatabase(ex.Message);
            }

        }


        public void  LineNotice(string msg)
        {

            string site = ConfigurationManager.AppSettings["site"].ToString();          
            string strSp = @"[dbo].[spSMD_LINE_NOTICE]";
            Hashtable prm = new Hashtable();
            prm.Add("NOTICE_SYS_ID", "UDI");
            prm.Add("NOTICE_SYS_NAME", "UDI");
            prm.Add("NOTICE_USER", "0092952^0206712^0057267^");
            prm.Add("NOTICE_SUBJECT", $"{site} 發生錯誤");
            prm.Add("NOTICE_CONTENT", $"{Parameter_DeviceId} {msg}");
            prm.Add("NOTICE_GRADE", "1");
            prm.Add("NOTICE_CREATE_MAN", $"{site}_sys");

            _dBConn.SqlSp("SMD", strSp, prm, Parameter_TaskId, Parameter_Step);
        }

        // 清除
        public void Agent_Clean()
        {

        }


        // 清除
        public async Task Agent_LocalClean()
        {

        }

        public async Task<DataSet> Execute()
        {
            DataSet dataSet = null;
            try
            {
                string strSp = @"[dbo].[spUDI_EXECUTE]";
                Hashtable ht_Query = new Hashtable();
                ht_Query.Add("GUID", Parameter_GUID);
                dataSet = await Task.Run(() => _dBConn.SqlSp("UDI", strSp, ht_Query, Parameter_TaskId, Parameter_Step));

            }
            catch (Exception ex)
            {
                LogToDatabase(ex.Message);
            }




            return dataSet;
        }

        /// <summary>
        /// 將 LCU0301 文件 寫入 TABLE
        /// </summary>
        /// <param name="prm"></param>  參數
        /// <param name="intItems"></param>  行數
        /// <returns></returns>
        #region 將 LCU0301 文件 寫入 TABLE
        public bool TxtInsertTable(Hashtable prm, ref int intItems)
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

        public async Task<bool> InsertUdiGetWithSqlBulkCopy(DataTable dataTable)
        {



             
            bool result = await _dBConn.InsertWithSqlBulkCopy("UDI", "[ftp].[UDI_GET]", dataTable, Parameter_GUID, Parameter_Step);
            return result;
        }

        public void LogToDatabase(string msg)
        {

            string strSp = @"[log].[spUDI_ERROR]";
            Hashtable ht_Query = new Hashtable();
            ht_Query.Add("GUID", Parameter_GUID);
            ht_Query.Add("MSG", msg);
            DataSet dataSet = _dBConn.SqlSp("UDI", strSp, ht_Query, Parameter_TaskId, Parameter_Step);




            //string strSql = @"INSERT [log].[UDI_ERROR](LOG_TIME
            //                                          ,GUID
            //                    		  ,DEVICE_ID
            //                    		  ,ORDER_ID
            //                    		  ,MSG)
            //                         SELECT GETDATE()
            //                             ,@GUID
            //                    		 ,@DEVICE_ID
            //                    		 ,@ORDER_ID
            //                    		 ,@MSG";
            //Hashtable ht_Query = new Hashtable();
            //ht_Query.Add("GUID", Parameter_GUID);
            //ht_Query.Add("DEVICE_ID", Parameter_DeviceId);
            //ht_Query.Add("ORDER_ID", Parameter_OrderId);
            //ht_Query.Add("MSG", msg);

            //DataSet dt = _dBConn.SqlQuery("UDI", strSql, ht_Query, Parameter_TaskId, Parameter_OrderType);
        }

        public string HandleType(string type)
        {
            string handleType = type switch
            {
                "0" => "Initial",
                "1" => "Dele",
                "2" => "Dele",
                "3" => "Rcv",
                "4" => "Rcv",
                "5" => "Send",
                "8" => "Send",
                "9" => "Send",
                "10" => "Send",
                "11" => "Send",
                "12" => "Send",
                "13" => "Send",
                "14" => "Send",
                "15" => "Send",
                "16" => "Send",
                "17" => "Send",
                "18" => "Send",
            };

            return handleType;
        }

        public string HandleName(string type)
        {
            string handleType = type switch
            {
                "0" => "執行 預設",
                "1" => "執行 實績刪除",
                "2" => "執行 指示刪除",
                "3" => "執行 取得實績",
                "4" => "執行 取得實績(完整)",
                "5" => "執行 生產指示",
                "8" => "執行 商品主檔",
                "9" => "執行 門市主檔",
                "10" => "執行 產地主檔",
                "11" => "執行 原材料主檔",
                "12" => "執行 註譯主檔",
                "13" => "執行 保存方法主檔",
                "14" => "執行 作業者主檔",
                "15" => "執行 托盤主檔",
                "16" => "執行 添加物主檔",
                "17" => "執行 廣告文主檔",
                "18" => "執行 班次主檔",
            };

            return handleType;
        }
    }
}
