using Agent_ClassLibrary.AppExceptionHandle;
using Agent_ClassLibrary.CommonQueryISHIDA;
using Agent_ClassLibrary.CommonQueryLCU;
using Agent_ClassLibrary.Ftp;
using Agent_ClassLibrary.FtpFileHandle;
using Agent_ClassLibrary.Gloab;
using Agent_ClassLibrary.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Runtime.InteropServices;
using System.Text;
using UDI_FTP_TERAKO_Agent.Handel.GetTxtFromTERAKO;
using UDI_FTP_TERAKO_Agent.Handel.GetTxtFromTERKO;

namespace UDI_FTP_TERAKO_Agent
{
    internal class Program
    {
        // 引入 WinAPI，用來取得和設定控制台模式
        #region 由於因為滑鼠點擊會變成選取中狀態而中止，固增加這段
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle); // 取得標準設備（如輸入、輸出）的控制代碼

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode); // 取得目前控制台模式

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode); // 設定控制台模式

        const int STD_INPUT_HANDLE = -10; // 表示標準輸入（鍵盤）的控制代碼
        const uint ENABLE_QUICK_EDIT_MODE = 0x0040; // 快速編輯模式標誌
        const uint ENABLE_EXTENDED_FLAGS = 0x0080;  // 擴展標誌，確保其他模式設定有效
        #endregion



        public static FtpClient ftpclient;  // 靜態引用,大家共享
        public static string FileDirectory = ""             //存檔路徑
                            , FileDirectory_SendBackup = ""      //傳送檔案備份
                            , FileDirectory_ReturnBackup = "";   //回收檔案備份
        static string appGuidExe = "", appGuid = "";        //每支程式以不同GUID當成Mutex名稱，可避免執行檔同名同姓的風險
        static async Task Main(string[] args)
        {

            //由於因為滑鼠點擊會變成選取中狀態而中止，固增加這段
            DisableQuickEditMode();

            //測試參數
            //string[] test = new string[5];
            //test[0] = "172.20.8.233";
            //test[1] = "1b7ff453-8b5e-434e-96c4-b7a12d113ead";
            //test[2] = "1";
            //test[3] = "251110101";
            //test[4] = "2350384f-7cf1-438e-ae78-7fbaa6fac787";
            //args = test;

            /*** 
              DI 注入
            ***/
            #region DI 注入
            var builder = new HostBuilder().ConfigureServices((hostcontext, services) =>
        {
            services.AddScoped<IDBConn, DBConn>();
            services.AddScoped<ICommonQueryLCU, CommonQueryLCU>();
            services.AddScoped<IAppExceptionHandle, AppExceptionHandle>();
            services.AddScoped<IGlobalUtility, GlobalUtility>();
            services.AddScoped<IFtpFileHndel, FtpFileHandel>();
            services.AddScoped<IGetTxtFromTERAKO, GetTxtFromTERAKO>();
        });
            var host = builder.Build();
            var AppExceptionHandle = host.Services.GetRequiredService<IAppExceptionHandle>();
            var globalUtility = host.Services.GetRequiredService<IGlobalUtility>();
            var ftpFileHandel = host.Services.GetRequiredService<IFtpFileHndel>();
            var getTxtFromTERAKO = host.Services.GetRequiredService<IGetTxtFromTERAKO>();
            #endregion


            #region 準備參數
            string ErrMsg = "";
            string paras = "";
            bool ProgramTermination = true;
            #endregion


            /*** 
            檢查傳進來的參數
            ***/
            #region 檢查傳進來的參數
            foreach (string arg in args)
            {
                paras += arg + ",";
            }

            if (args.Length != 5)
            {

                globalUtility.LogToFile(appGuid + " 參數不正確  :" + paras);
                return;
            }
            #endregion

            /*** 
            同設備，同Flow，不需要重複執行    
             ***/
            //Agent_LCU專屬Mutex
            appGuidExe = Process.GetCurrentProcess().ProcessName + ".exe";
            //建立專屬ID，因同裝置, 不需要重複執行
            appGuid = appGuidExe; //+ "^" + args[1] + "^" + args[2] + "^" + args[3];

            //如果要做到跨Session唯一，名稱可加入"Global\"前綴字
            //如此即使用多個帳號透過Terminal Service登入系統
            //整台機器也只能執行一份
            using (Mutex m = new Mutex(false, "Global\\" + appGuid))
            {

                /***
                接收參數
                ***/
                #region 接收參數
                globalUtility.Parameter_Url = args[0];
                globalUtility.Parameter_GUID = args[1];
                globalUtility.Parameter_OrderType = args[2];
                globalUtility.Parameter_OrderId = args[3];
                globalUtility.Parameter_TaskId = args[4];
                globalUtility.Parameter_DeviceId = "LCU01";
                #endregion


                if (!m.WaitOne(0, true))
                {
                    globalUtility.LogToFile(appGuid + " 同區域同裝置, 不用重複執行.");
                    globalUtility.LogToDatabase(appGuid + " 同區域同裝置, 不用重複執行.");
                    return;
                }

                globalUtility.Parameter_Step = globalUtility.Parameter_OrderType;
                globalUtility.LogToFile($"args[0] = {args[0]} ， args[1] = {args[1]} ， args[2] = {args[2]} ， args[3] = {args[3]}，args[4] = {args[4]}");

                /***
                  建立 備份 資料夾
                ***/
                #region 建立 備份 資料夾
                FileDirectory = AppDomain.CurrentDomain.BaseDirectory + "TERAKO" + "\\" + globalUtility.Parameter_GUID;
                FileDirectory_SendBackup = FileDirectory + "\\SendDataBackup";
                FileDirectory_ReturnBackup = FileDirectory + "\\RcvDataBackup";
                Directory.CreateDirectory(FileDirectory);
                Directory.CreateDirectory(FileDirectory_SendBackup);
                Directory.CreateDirectory(FileDirectory_ReturnBackup);
                #endregion

                /***
                執行 Execute
                ***/
                #region Execute
                ErrMsg = await ftpFileHandel.ExecuteAction();

                if (ErrMsg != "")
                {
                    globalUtility.LogToFile(ErrMsg);
                    globalUtility.LogToDatabase(ErrMsg);
                    return;
                };
                #endregion


                // 新增關閉視窗事件
                #region 新增關閉視窗事件
                AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
                #endregion


                /***
                   意外錯誤處理
                ***/
                #region 意外錯誤處理
                AppExceptionHandle.AppExceptionHandle();
                #endregion


                /***
                   ftp 連線
                ***/
                #region ftp 連線
                globalUtility.FTP_Connectoin(ref ftpclient);
                #endregion

                /***
                取得 給 設備文件名稱 和 從設備回傳的文件名稱
                ***/
                #region 給 設備文件名稱 和 從設備回傳的文件名稱
                ErrMsg = await ftpFileHandel.GetftpOrder();

                if (ErrMsg != "")
                {
                    globalUtility.LogToFile(ErrMsg);
                    globalUtility.LogToDatabase(ErrMsg);
                    return;
                };

                // 沒有 PutFileName，代表只是執行命令，不產文件
                if (ftpFileHandel.PutFileName == "")
                {
                    globalUtility.LogToFile("執行命令");
                    ProgramTermination = false;
                    return;
                }
                #endregion

                #region 要產生文件名稱
                //產生文件的資料夾，備份文件的資料夾
                string FileName = ftpFileHandel.PutFileName //檔案名稱
                    , FileNameReturn = ftpFileHandel.GetFileName
                    , PutFilePath = Program.FileDirectory + @"\" + FileName     //輸出文字檔目錄
                    , GetFilePath = Program.FileDirectory + @"\" + FileNameReturn
                    , FilePath_SendBackup = Program.FileDirectory_SendBackup + @"\" + DateTime.Now.ToString("yyyyMMddHHmmss") + FileName   //備份檔案
                    , FilePath_ReturnBackup = Program.FileDirectory_ReturnBackup + @"\" + DateTime.Now.ToString("yyyyMMddHHmmss") + FileNameReturn;  //備份檔案

                #endregion



                Hashtable hashtable = new Hashtable();
                hashtable.Add("GUID", globalUtility.Parameter_GUID);


                try
                {
                    /***
                    取得 文字檔資料，並產生文件給設備，並判斷 PostGet 如果非空字串，將設備回傳的文件資料寫入 GET 資料表
                    ***/
                    DataSet dsPutTxt = await ftpFileHandel.GetFtpPutTxt(hashtable);
                    if (dsPutTxt.Tables.Count > 0)
                    {
                        if (dsPutTxt.Tables[0].Rows.Count > 0)
                        {

                            #region 輸出指定編號的TXT
                            using (StreamWriter sw_OutPutTXT = new StreamWriter(PutFilePath, false, Encoding.GetEncoding("big5")))
                            {
                                string data = "";
                                foreach (DataRow row in dsPutTxt.Tables[0].Rows)
                                {
                                    data = row["TXT"].ToString() + "\n";
                                    sw_OutPutTXT.Write(data);
                                    data = "";
                                }
                                data += "\n";
                            }


                            //備分 文件 到 輩分資料夾
                            File.Copy(PutFilePath, FilePath_SendBackup);
                            #endregion

                            // 清除 遠端FTP 文件
                            #region 清除ftp舊資料
                            var files = ftpclient.directoryListSimple("");
                            foreach (var file in files)
                            {
                                if (file == FileName)
                                {
                                    Program.ftpclient.delete(FileName);
                                    globalUtility.LogToFile(" " + FileName + "刪除完成");
                                }
                            }
                            #endregion

                            // 上傳文件 並且加上 .tmp 到備份文件               
                            Program.ftpclient.upload(FileName + ".tmp", PutFilePath);

                            // 檢查是否有成功上傳文件到遠端 FTP
                            ErrMsg = globalUtility.FTPCheckFileUploadOK(FileName + ".tmp", ref Program.ftpclient, 0);
                            if (ErrMsg != "")
                            {
                                globalUtility.LogToFile(ErrMsg);
                                globalUtility.LogToDatabase(ErrMsg);
                                return;
                            }

                            // 上傳文件成功後，將 .tmp 移除
                            Program.ftpclient.rename(FileName + ".tmp", FileName);
                            globalUtility.LogToFile(" " + FileName + "下傳成功");


                            // PostGet 非空字串，代表要回寫到 GET TABLE
                            if (ftpFileHandel.PostGet == "")
                            {

                                // 先確認文件有沒有產出，在判斷文件有沒有被搬走
                                ErrMsg = globalUtility.FTPCheckFileUploadOK(FileNameReturn, ref Program.ftpclient, 1);
                                if (ErrMsg != "")
                                {

                                    globalUtility.LogToFile(ErrMsg);
                                    globalUtility.LogToDatabase(ErrMsg);
                                    return;
                                }
                            }
                            else
                            {


                                // 先確認文件有沒有產出
                                ErrMsg = globalUtility.FTPCheckFileUploadOK(FileNameReturn, ref Program.ftpclient, 0);
                                if (ErrMsg != "")
                                {
                                    globalUtility.LogToFile(ErrMsg);
                                    globalUtility.LogToDatabase(ErrMsg);
                                    return;
                                }

                                // 將文件下載到本地端
                                Program.ftpclient.download(FileNameReturn, GetFilePath);

                                // 檢查是否有成功下載到本地端
                                if (File.Exists(GetFilePath))
                                {
                                    globalUtility.LogToFile($" {FileNameReturn} 下載成功");
                                }
                                else
                                {
                                    ErrMsg = $" {FileNameReturn} 下載失敗";
                                    globalUtility.LogToFile(ErrMsg);
                                    globalUtility.LogToDatabase(ErrMsg);
                                    return;
                                }
                                // 搬到備分
                                File.Copy(GetFilePath, FilePath_ReturnBackup);

                                // 寫入 Table
                                await getTxtFromTERAKO.GetTxtToTable(FilePath_ReturnBackup);

                                Program.ftpclient.delete(FileNameReturn);

                                globalUtility.LogToFile(" " + FileNameReturn + "刪除完成");
                            }

                            // 改變狀態
                            //await  globalUtility.UpdateState();
                            // 清除 遠端FTP 文件
                        }
                        else
                        {
                            ErrMsg = "取不到 PUT 的資料";
                            globalUtility.LogToFile(ErrMsg);
                            globalUtility.LogToDatabase(ErrMsg);
                        }
                    }

                    ProgramTermination = false;
                }
                catch (Exception ex)
                {

                    globalUtility.LogToFile(ex.Message);
                    globalUtility.LogToDatabase(ex.Message);
                }
            }

            // 執行結束的事件
            void OnProcessExit(object sender, EventArgs e)
            {
                if (ProgramTermination == true)
                {
                    globalUtility.LogToFile("異常中斷");
                    globalUtility.LogToDatabase("異常中斷");
                }
                else
                {
                    globalUtility.UpdateState1();
                    globalUtility.LogToFile("執行結束");
                }

            }
        }

        //由於因為滑鼠點擊會變成選取中狀態而中止，固增加這段
        static void DisableQuickEditMode()
        {
            // 取得標準輸入控制代碼（鍵盤輸入的控制代碼）
            IntPtr consoleHandle = GetStdHandle(STD_INPUT_HANDLE);

            // 檢查是否成功取得目前模式
            if (GetConsoleMode(consoleHandle, out uint mode))
            {
                // 禁用快速編輯模式
                mode &= ~ENABLE_QUICK_EDIT_MODE; // 清除快速編輯模式位（&= ~0x0040）

                // 設定擴展標誌，確保其他控制台模式能正常工作
                mode |= ENABLE_EXTENDED_FLAGS; // |= 0x0080，啟用擴展標誌

                // 套用新的模式設定
                SetConsoleMode(consoleHandle, mode);
            }
        }
    }
}
