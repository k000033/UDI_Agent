using Agent_ClassLibrary.AppExceptionHandle;
using Agent_ClassLibrary.Ftp;
using Agent_ClassLibrary.Gloab;
using Agent_ClassLibrary.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using UDI_SP_WAS_Agent.Handel.DeleToWAS;
using UDI_SP_WAS_Agent.Handel.RcvFromWAS;
using UDI_SP_WAS_Agent.Handel.SendToWAS;

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
             * ID 注入
             * **/

            var builder = new HostBuilder().ConfigureServices((hostcontext, services) =>
            {
                services.AddScoped<IDBConn, DBConn>();
                services.AddScoped<IAppExceptionHandle, AppExceptionHandle>();
                services.AddScoped<IGlobalUtility, GlobalUtility>();
                services.AddScoped<IDeleToWAS, DeleToWAS>();
                services.AddScoped<IRcvFromWAS, RcvFromWAS>();
                services.AddScoped<ISendToWAS, SendToWAS>();
            });

            var host = builder.Build();
            var global = host.Services.GetRequiredService<IGlobalUtility>();
            var AppExceptionHandle = host.Services.GetRequiredService<IAppExceptionHandle>();
            var DeleToWas = host.Services.GetRequiredService<IDeleToWAS>();
            var RcvHandel = host.Services.GetRequiredService<IRcvFromWAS>();
            var SendToWas = host.Services.GetRequiredService<ISendToWAS>();
            // 測試參數
            //string[] test = new string[5];
            //test[0] = "123";
            //test[1] = "400b079f-1b20-4150-847f-53778f11a5a3";
            //test[2] = "5";
            //test[3] = "242550101";
            //test[4] = "07eee55a-d3d2-473c-8d32-d1f8bc0b5b2c";
            //args = test;
            //args[0] = 123 ， args[1] = 400b079f-1b20-4150-847f-53778f11a5a3 ， args[2] = 5 ， args[3] = 242550101，args[4] = 07eee55a-d3d2-473c-8d32-d1f8bc0b5b2c



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

                global.Agent_WriteLog(appGuid + " 參數不正確  :" + paras);
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
                建立 備份 資料夾
                ***/
                #region 建立 備份 資料夾
                //FileDirectory = AppDomain.CurrentDomain.BaseDirectory + "WAS" + "\\" + global.Parameter_GUID;
                //FileDirectory_SendBackup = FileDirectory + "\\SendDataBackup";
                //FileDirectory_ReturnBackup = FileDirectory + "\\RcvDataBackup";
                //Directory.CreateDirectory(FileDirectory);
                //Directory.CreateDirectory(FileDirectory_SendBackup);
                //Directory.CreateDirectory(FileDirectory_ReturnBackup);
                #endregion

                /***
                意外錯誤處理
                ***/
                #region 意外錯誤處理
                AppExceptionHandle.AppExceptionHandle();
                #endregion

                #region 清除過去的Log
                await global.Agent_LocalClean();
                #endregion

                /***
                設備啟動程序
                ***/
                #region 設備啟動程序
                if (global.HandleType(global.Parameter_OrderType) == "Initial")
                {
                    global.Parameter_Step = global.Parameter_OrderType;
                    string instructions = global.HandleName(global.Parameter_OrderType);
                    global.Agent_WriteLog(instructions);
                    await global.Execute();
                    await global.UpdateState();
                }
                #endregion





                /***
                 清除實績、清除實績、清除主檔
                ***/
                //string[] DeleHandelList = ["1", "2"];
                if (global.HandleType(global.Parameter_OrderType) == "Dele")
                {
                    try
                    {
                        global.Parameter_Step = global.Parameter_OrderType;
                        string instructions = global.HandleName(global.Parameter_OrderType);
                        //string instructions = global.Parameter_OrderType switch
                        //{
                        //    "1" => "執行 清除實績",
                        //    "2" => "執行 清除指示",
                        //    _ => ""
                        //};
                        global.Agent_WriteLog(instructions);
                        var result = await DeleToWas.MainFunction(global.Parameter_OrderType);
                        global.Agent_WriteLog($"回傳訊息 : {result}");
                    }
                    catch (Exception ex)
                    {
                        global.Agent_WriteLog($"錯誤訊息 : {ex.Message}");
                    }
                }

                /***
                回收實績
                ***/
                #region 回收實績
                //string[] RcvHandelList = ["3", "4"];
                if (global.HandleType(global.Parameter_OrderType) == "Rcv")
                {
                    try
                    {
                        global.Parameter_Step = global.Parameter_OrderType;
                        string instructions = global.HandleName(global.Parameter_OrderType);
                        //string instructions = global.Parameter_OrderType switch
                        //{
                        //    "3" => "執行 取得實績",
                        //    "4" => "執行 取得實績(完整)",
                        //};

                        global.Agent_WriteLog(instructions);

                        var result = await RcvHandel.MainFunction(global.Parameter_OrderType);
                        global.Agent_WriteLog(result);
                    }
                    catch (Exception ex)
                    {
                        global.Agent_WriteLog($"錯誤訊息 : {ex.Message}");
                    }
                }
                #endregion

                #region 下生產指示，主檔 (生產指示、門市、商品、產地，原材料，註譯........)
                //string[] SendHandelList = ["5", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18"];
                if (global.HandleType(global.Parameter_OrderType) == "Send")
                {
                    try
                    {
                        global.Parameter_Step = global.Parameter_OrderType;
                        string instructions = global.HandleName(global.Parameter_OrderType);
                        //string instructions = global.Parameter_OrderType switch
                        //{
                        //    "5" => "執行 生產指示",
                        //    "8" => "執行 商品主檔",
                        //    "9" => "執行 門市主檔",
                        //    "10" => "執行 產地主檔",
                        //    "11" => "執行 原材料主檔",
                        //    "12" => "執行 註譯主檔",
                        //    "13" => "執行 保存方法主檔",
                        //    "14" => "執行 作業者主檔",
                        //    "15" => "執行 托盤主檔",
                        //    "16" => "執行 添加物主檔",
                        //    "17" => "執行 廣告文主檔",
                        //    "18" => "執行 班次主檔",
                        //    _ => "" // 默认情况，返回 null 或其他适当的值
                        //};

                        global.Agent_WriteLog(instructions);
                        var result = await SendToWas.MainFunction(global.Parameter_OrderType);
                        global.Agent_WriteLog($"回傳訊息 : {result}");
                    }
                    catch (Exception ex)
                    {
                        global.Agent_WriteLog($"錯誤訊息 : {ex.Message}");
                    }
                }
                #endregion
            }
        }
    }
}
