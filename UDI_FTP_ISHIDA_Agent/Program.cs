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
using System.Runtime.InteropServices;
using System.Text;

using UDI_FTP_ISHIDA_Agent.Handel.GetTxtFromISHIDA;


namespace UDI_FTP_ISHIDA_Agent
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
            //test[1] = "55D21235-A8B3-46DF-A56B-55F0C296E2DA";
            //test[2] = "4";
            //test[3] = "251100103";
            //test[4] = "FEFDB4FF-AD98-4BEB-BCA2-F0A4FAE9E42A";
            //args = test;


            /**
             *  DI 注入
            */
            #region DI 注入
            var builder = new HostBuilder().ConfigureServices((hostcontext, services) =>
            {
                services.AddScoped<IDBConn, DBConn>();
                services.AddScoped<IAppExceptionHandle, AppExceptionHandle>();
                services.AddScoped<IFtpFileHndel, FtpFileHandel>();
                services.AddScoped<IGlobalUtility, GlobalUtility>();
                services.AddScoped<IGetTxtFromISHIDA, GetTxtFromISHIDA>();

            });
            var host = builder.Build();

            var AppExceptionHandle = host.Services.GetRequiredService<IAppExceptionHandle>();
            var globalUtility = host.Services.GetRequiredService<IGlobalUtility>();
            var ftpFileHandel = host.Services.GetRequiredService<IFtpFileHndel>();
            var getTxtFromISHIDA = host.Services.GetRequiredService<IGetTxtFromISHIDA>();
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
            appGuid = appGuidExe; //+ "^" + args[3]; //"^" + args[2] + "^" + args[3];

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
                globalUtility.Parameter_DeviceId = "PCRS01";
                #endregion


                if (!m.WaitOne(0, true))
                {
                    globalUtility.LogToFile(appGuid + " 同區域同裝置, 不用重複執行.");
                    globalUtility.LogToDatabase(appGuid + " 同區域同裝置, 不用重複執行.");
                    return;
                }


                globalUtility.Parameter_Step = globalUtility.Parameter_OrderType;
                globalUtility.LogToFile($"args[0] = {args[0]} ， args[1] = {args[1]} ， args[2] = {args[2]} ， args[3] = {args[3]}，args[4] = {args[4]}");

                //建立 備份 資料夾
                #region 建立 備份 資料夾
                FileDirectory = AppDomain.CurrentDomain.BaseDirectory + "ISHIDA" + "\\" + globalUtility.Parameter_GUID;
                FileDirectory_SendBackup = FileDirectory + "\\SendDataBackup";
                FileDirectory_ReturnBackup = FileDirectory + "\\RcvDataBackup";
                Directory.CreateDirectory(FileDirectory);
                Directory.CreateDirectory(FileDirectory_SendBackup);
                Directory.CreateDirectory(FileDirectory_ReturnBackup);
                #endregion

                /***
                  執行 Execute，產生文件到 ftp.put 資料表
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

                //#region 清除過去的Log
                //await globalUtility.Agent_LocalClean();
                //#endregion

                /***
                FTP 連線
                ***/
                #region FTP 連線
                globalUtility.FTP_Connectoin(ref ftpclient);
                #endregion




                /***
                 取得 給 設備文件名稱 和 從設備回傳的文件名稱
                ***/
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




                /***
                產生文件的資料夾，備份文件的資料夾 ，並判斷
                ***/
   
                //開始下傳TXT, 根據指定的文字檔和裝置
                string FileName = "ISHIDA_" + ftpFileHandel.PutFileName   //檔案名稱          
                     , FileNameReturn = "ISHIDA_" + ftpFileHandel.GetFileName
                     , PutFilePath = Program.FileDirectory + @"\" + FileName     //輸出文字檔目錄
                     , GetFilePath = Program.FileDirectory + @"\" + FileNameReturn
                     , FilePath_SendBackup = Program.FileDirectory_SendBackup + @"\" + DateTime.Now.ToString("yyyyMMddHHmmss") + FileName   //備份檔案
                     , FilePath_ReturnBackup = Program.FileDirectory_ReturnBackup + @"\" + DateTime.Now.ToString("yyyyMMddHHmmss") + FileNameReturn;  //備份檔案


                Hashtable hashtable = new Hashtable();
                hashtable.Add("GUID", globalUtility.Parameter_GUID);


                try
                {
                    /***
                    取得 文字檔資料，並產生文件給設備，並判斷 PostGet 如果非空字串，將設備回傳的文件資料寫入 GET 資料表
                    ***/
                    #region 取得 文字檔資料，並產生文件給設備
                    DataSet dsPutTxt = await ftpFileHandel.GetFtpPutTxt(hashtable);
                    if (dsPutTxt.Tables.Count > 0)
                    {
                        if (dsPutTxt.Tables[0].Rows.Count > 0)
                        {

                            // 輸出文字檔
                            using (StreamWriter sw_OutPutTXT = new StreamWriter(PutFilePath, false, System.Text.Encoding.UTF8))
                            {
                                string data = "";

                                data += "\n";   //取代欄位定義, 第一行留白, 第二行起才是實際資料

                                foreach (DataRow row in dsPutTxt.Tables[0].Rows)
                                {
                                    data = row["TXT"].ToString() + "\n";
                                    sw_OutPutTXT.Write(data);
                                    data = "";
                                }
                            }

                            // 備份文件到 備份資料夾
                            File.Copy(PutFilePath, FilePath_SendBackup);


                            // 清除ftp 文件
                            var files = ftpclient.directoryListSimple("");
                            foreach (var file in files)
                            {
                                if (file == FileName)
                                {
                                    Program.ftpclient.delete(FileName);
                                    globalUtility.LogToFile(" " + FileName + "刪除完成");
                                };

                                if (file == FileNameReturn)
                                {
                                    Program.ftpclient.delete(FileNameReturn);
                                    globalUtility.LogToFile(" " + FileNameReturn + "刪除完成");
                                }
                            }

                            //  上傳文件並加 .tmp 副檔名到  遠端 FTP 資料夾
                            Program.ftpclient.upload(FileName + ".tmp", PutFilePath);

                            // 檢查是否上傳完畢，
                            ErrMsg = globalUtility.FTPCheckFileUploadOK(FileName + ".tmp", ref Program.ftpclient, 0);
                            if (ErrMsg != "")
                            {
                                globalUtility.LogToFile(ErrMsg);
                                globalUtility.LogToDatabase(ErrMsg);
                                return;
                            }

                            // 上傳完畢，將 .tmp 移除
                            Program.ftpclient.rename(FileName + ".tmp", FileName);
                            globalUtility.LogToFile($" {FileName} 上傳成功");


                            // 檢查檔案是否已經成功上傳,ISHIDA會回傳【結果】
                            // 判斷 遠端FTP 是否有產生結果文件\
                            ErrMsg = globalUtility.FTPCheckFileUploadOK(FileNameReturn, ref Program.ftpclient, 0);
                            if (ErrMsg != "")
                            {
                                globalUtility.LineNotice(ErrMsg);
                                globalUtility.LogToFile(ErrMsg);
                                globalUtility.LogToDatabase(ErrMsg);
                                return;
                            }

                           
                            Thread.Sleep(2000);


                            // 將結果文件下載到本地端
                            globalUtility.LogToFile("將結果文件下載到本地端");
                            Program.ftpclient.download(FileNameReturn, GetFilePath);
                            // 檢查是否下載完成
                            if (File.Exists(GetFilePath))
                            {
                                globalUtility.LogToFile(" " + FileNameReturn + " 下載成功");
                            }
                            else
                            {
                                ErrMsg = " " + FileNameReturn + " 下載失敗";
                                globalUtility.LogToFile(ErrMsg);
                                return;
                            }

                            //備份 結果檔案到 備分資料夾
                            File.Copy(GetFilePath, FilePath_ReturnBackup);


                            // 如果非空字串，代表要將回傳的文件寫入資料表
                            if (ftpFileHandel.PostGet == "")
                            {
                                getTxtFromISHIDA.GetMessageTxt(GetFilePath);
                            }
                            else
                            {
                               await getTxtFromISHIDA.GetTxtToTable(GetFilePath);
                            }

                            // 修改狀態
                            //await globalUtility.UpdateState();
                            // 清除ftp 文件
                            files = ftpclient.directoryListSimple("");
                            foreach (var file in files)
                            {
                                if (file == FileName)
                                {
                                    Program.ftpclient.delete(FileName);
                                    globalUtility.LogToFile(" " + FileName + "刪除完成");
                                };

                                if (file == FileNameReturn)
                                {
                                    Program.ftpclient.delete(FileNameReturn);
                                    globalUtility.LogToFile(" " + FileNameReturn + "刪除完成");
                                }
                            }
                        }
                        else
                        {
                            ErrMsg = "取不到 PUT 的資料";
                            globalUtility.LogToFile(ErrMsg);
                            globalUtility.LogToDatabase(ErrMsg);

                        }
                    }
                    #endregion
                    ProgramTermination = false;
                }
                catch (Exception ex)
                {

                    globalUtility.LogToFile(ex.Message);
                    globalUtility.LogToDatabase(ex.Message);
                }
            }

            // 關閉事件
            async void OnProcessExit(object sender, EventArgs e)
            {
                if (ProgramTermination == true)
                {
                    globalUtility.LogToFile("意外中斷");
                    globalUtility.LogToDatabase("意外中斷");
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
