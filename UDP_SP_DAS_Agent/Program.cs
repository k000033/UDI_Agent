using Agent_ClassLibrary.AppExceptionHandle;
using Agent_ClassLibrary.CommonQueryLCU;
using Agent_ClassLibrary.Ftp;
using Agent_ClassLibrary.Gloab;
using Agent_ClassLibrary.Service;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using UDI_SP_DAS_Agent.Handel.DeleToDAS;
using UDI_SP_DAS_Agent.Handel.RcvFromDas;
using UDI_SP_DAS_Agent.Handel.SendToDAS;

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
             * ID 注入
             * **/

            var builder = new HostBuilder().ConfigureServices((hostcontext, services) =>
            {
                services.AddScoped<IDBConn, DBConn>();
                services.AddScoped<IAppExceptionHandle, AppExceptionHandle>();
                services.AddScoped<IGlobalUtility, GlobalUtility>();
                services.AddScoped<IDeleToDAS, DeleToDAS>();
                services.AddScoped<IRcvFromDAS, RcvFromDAS>();
                services.AddScoped<ISendToDAS, SendToDAS>();
            });

            var host = builder.Build();
            var global = host.Services.GetRequiredService<IGlobalUtility>();
            var AppExceptionHandle = host.Services.GetRequiredService<IAppExceptionHandle>();
            var DeleToDas = host.Services.GetRequiredService<IDeleToDAS>();
            var RcvHandel = host.Services.GetRequiredService<IRcvFromDAS>();
            var SendToDas = host.Services.GetRequiredService<ISendToDAS>();
            //測試參數
            //string[] test = new string[5];
            //test[0] = "172.31.31.250";
            //test[1] = "445b67e0-8288-4951-93cb-8aa90c825222";
            //test[2] = "2";
            //test[3] = "243220102";
            //test[4] = "e48373a0-f7cc-4986-a1cd-3887728ffa2f";
            //args = test;

            //args[0] = 172.20.17.148 ， args[1] = 3f11b5e4-eb16-4b78-9ab6-4787b8718554 ， args[2] = 3 ， args[3] = 242510101，args[4] = 22148219-0bff-4c2b-bad6-d94f1541b106

            // args[0] = 172.20.17.148 ， args[1] = 7b5d6441-e867-4dbf-a676-58dbecf9af3e ， args[2] = 3 ， args[3] = 242480101，args[4] = 98a7125a-ed47-45fa-8135-6e1cea855027

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
                global.Parameter_DeviceId = "DAS01";
                #endregion

                global.Parameter_Step = global.Parameter_OrderType;
                global.Agent_WriteLog($"args[0] = {args[0]} ， args[1] = {args[1]} ， args[2] = {args[2]} ， args[3] = {args[3]}，args[4] = {args[4]}");


                /***
                建立 備份 資料夾
                ***/
                #region 建立 備份 資料夾
                //FileDirectory = AppDomain.CurrentDomain.BaseDirectory + "DAS" + "\\" + global.Parameter_TaskId;
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
                if (global.HandleType(global.Parameter_OrderType)== "Initial")
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
                        // 取得指令名稱
                        string instructions = global.HandleName(global.Parameter_OrderType);
                        global.Agent_WriteLog(instructions);
                        // 執行相對應的指令
                        var result = await DeleToDas.MainFunction(global.Parameter_OrderType);
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
                        // 取得指令名稱
                        string instructions = global.HandleName(global.Parameter_OrderType);
                        global.Agent_WriteLog(instructions);
                        // 執行相對應的指令
                        var result = await RcvHandel.MainFunction(global.Parameter_OrderType);
                        global.Agent_WriteLog($"回傳訊息 : {result}");
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
                        global.Agent_WriteLog(instructions);
                        var result = await SendToDas.MainFunction(global.Parameter_OrderType);
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
