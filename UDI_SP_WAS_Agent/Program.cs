using Agent_ClassLibrary.AppExceptionHandle;
using Agent_ClassLibrary.Ftp;
using Agent_ClassLibrary.Gloab;
using Agent_ClassLibrary.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using UDI_SP_WAS_Agent.Handel.MainFunction;


namespace UDI_SP_WAS_Agent
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
                services.AddScoped<IMainFunction, MainFunction>();
            });

            var host = builder.Build();
            var global = host.Services.GetRequiredService<IGlobalUtility>();
            var AppExceptionHandle = host.Services.GetRequiredService<IAppExceptionHandle>();
            var mainFunction = host.Services.GetRequiredService<IMainFunction>();
            #endregion

            // 測試參數
            #region 測試參數
            //string[] test = new string[5];
            //test[0] = "123";
            //test[1] = "400b079f-1b20-4150-847f-53778f11a5a3";
            //test[2] = "5";
            //test[3] = "242550101";
            //test[4] = "07eee55a-d3d2-473c-8d32-d1f8bc0b5b2c";
            //args = test;
            //args[0] = 123 ， args[1] = 400b079f-1b20-4150-847f-53778f11a5a3 ， args[2] = 5 ， args[3] = 242550101，args[4] = 07eee55a-d3d2-473c-8d32-d1f8bc0b5b2c
            #endregion


            string paras = "";

            /*** 
            檢查傳進來的參數
            ***/
            #region 檢查參數
            foreach (string arg in args)
            {
                paras += arg + ",";
            }

            if (args.Length != 5)
            {

                global.Agent_WriteLog(appGuid + " 參數不正確  :" + paras);
                return;
            }
            #endregion

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
                    global.Agent_WriteLog(appGuid + " 同區域同裝置, 不用重複執行.");
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
                global.Parameter_DeviceId = "WAS01";
                #endregion

                global.Parameter_Step = global.Parameter_OrderType;
                global.Agent_WriteLog($"args[0] = {args[0]} ， args[1] = {args[1]} ， args[2] = {args[2]} ， args[3] = {args[3]}，args[4] = {args[4]}");


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
                    string instructions = global.HandleName(global.Parameter_OrderType);
                    global.Agent_WriteLog(instructions);
                    var result = await mainFunction.MainFun(global.Parameter_OrderType);
                    global.Agent_WriteLog($"回傳訊息 : {result}");
                }
                catch (Exception ex)
                {
                    global.Agent_WriteLog($"錯誤訊息 : {ex.Message}");
                }

            }
        }
    }
}
