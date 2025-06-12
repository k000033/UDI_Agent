using Agent_ClassLibrary.AppExceptionHandle;
using Agent_ClassLibrary.CommonQueryLCU;
using Agent_ClassLibrary.Ftp;
using Agent_ClassLibrary.Gloab;
using Agent_ClassLibrary.Service;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using UDI_SP_DAS_Agent.Handel.MainFn;


namespace UDP_SP_DAS_Agent
{
    internal class Program
    {
        public static FtpClient ftpclient;  // 靜態引用,大家共享
        public static string FileDirectory = ""             //存檔路徑
                            , FileDirectory_SendBackup = ""      //傳送檔案備份
                            , FileDirectory_ReturnBackup = "";   //回收檔案備份
        static string appGuidExe = "", appGuid = "";        //每支程式以不同GUID當成Mutex名稱，可避免執行檔同名同姓的風險
        static async Task Main(string[] args)
        {

            /**
             * DI 注入
             * **/
            #region DI 注入
            var builder = new HostBuilder().ConfigureServices((hostcontext, services) =>
            {
                services.AddScoped<IDBConn, DBConn>();
                services.AddScoped<IAppExceptionHandle, AppExceptionHandle>();
                services.AddScoped<IGlobalUtility, GlobalUtility>();
                services.AddScoped<IMainFn, MainFn>();
            });

            var host = builder.Build();
            var global = host.Services.GetRequiredService<IGlobalUtility>();
            var AppExceptionHandle = host.Services.GetRequiredService<IAppExceptionHandle>();
            var mainFn=host.Services.GetRequiredService<IMainFn>();
            #endregion


            //測試參數
            #region 測試參數
            //string[] test = new string[5];
            //test[0] = "172.31.31.250";
            //test[1] = "445b67e0-8288-4951-93cb-8aa90c825222";
            //test[2] = "2";
            //test[3] = "243220102";
            //test[4] = "e48373a0-f7cc-4986-a1cd-3887728ffa2f";
            //args = test;
            #endregion

            string paras = "";
            /*** 
            檢查傳進來的參數
            ***/
            foreach (string arg in args)
            {
                paras += arg + ",";
            }

            if (args.Length != 5)
            {

                global.LogToFile(appGuid + " 參數不正確  :" + paras);
                return;
            }

            /*** 
            同設備，同Flow，不需要重複執行    
            ***/
            //Agent_LCU專屬Mutex
            appGuidExe = Process.GetCurrentProcess().ProcessName + ".exe";
            //建立專屬ID，因同裝置, 不需要重複執行
            appGuid = appGuidExe + "^" + args[1] + "^" + args[2] + "^" + args[3];


            using (Mutex m = new Mutex(false, "Global\\" + appGuid))
            {
                if (!m.WaitOne(0, true))
                {
                    global.LogToFile(appGuid + " 同區域同裝置, 不用重複執行.");
                    return;
                }

                /***
                 接收參數
                ***/
                #region 接收參數
                global.Parameter_Url = args[0];
                global.Parameter_GUID = args[1];
                global.Parameter_OrderType = args[2];
                global.Parameter_OrderId = args[3];
                global.Parameter_TaskId = args[4];
                global.Parameter_DeviceId = "DAS01";
                #endregion

                global.Parameter_Step = global.Parameter_OrderType;
                global.LogToFile($"args[0] = {args[0]} ， args[1] = {args[1]} ， args[2] = {args[2]} ， args[3] = {args[3]}，args[4] = {args[4]}");


                /***
                意外錯誤處理
                ***/
                #region 意外錯誤處理
                AppExceptionHandle.AppExceptionHandle();
                #endregion

                /***
                 執行 執行動作的特定的SP，壓完成時間
                 ***/
                try
                {
                    global.Parameter_Step = global.Parameter_OrderType;
                    // 取得指令名稱
                    string instructions = global.HandleName(global.Parameter_OrderType);
                    global.LogToFile(instructions);
                    // 執行相對應的指令
                    var result = await mainFn.MainFunction(global.Parameter_OrderType);
                    global.LogToFile($"回傳訊息 : {result}");
                }
                catch (Exception ex)
                {
                    global.LogToFile($"錯誤訊息 : {ex.Message}");
                    global.LogToDatabase(ex.Message);
                }

            }
        }
    }
}
