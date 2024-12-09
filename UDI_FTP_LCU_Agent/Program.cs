
using Agent_ClassLibrary.AppExceptionHandle;
using Agent_ClassLibrary.CommonQueryLCU;
using Agent_ClassLibrary.Ftp;
using Agent_ClassLibrary.Service;


using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;


using UDI_FTP_LCU_Agent.Handel.DeleToLCU;
using UDI_FTP_LCU_Agent.Handel.RcvFromLCU;
using UDI_FTP_LCU_Agent.Handel.SendToLCU;


namespace UDI_FTP_LCU_Agent
{
    internal class Program
    {
        public static FtpClient ftpclient;  // 靜態引用,大家共享
        public static string FileDirectory = ""             //存檔路徑
                            , FileDirectory_SendBackup = ""      //傳送檔案備份
                            , FileDirectory_ReturnBackup = "";   //回收檔案備份

        //static string appGuidExe = "", appGuid = "";        //每支程式以不同GUID當成Mutex名稱，可避免執行檔同名同姓的風險
        static void Main(string[] args)
        {
            #region DI 注入
            var builder = new HostBuilder().ConfigureServices((hostcontext, services) =>
            {
                services.AddScoped<ICommonQueryLCU, CommonQueryLCU>();
                services.AddScoped<IDBConn, DBConn>();

                services.AddScoped<IAppExceptionHandle, AppExceptionHandle>();
                services.AddScoped<IRcvFromLCU, RcvFromLCU>();
                services.AddScoped<ISendToLCU, SendToLCU>();
                services.AddScoped<IDeteToLCU, DeleToLCU>();
            });
            var host = builder.Build();

            var ComQryLCU = host.Services.GetRequiredService<ICommonQueryLCU>();
            var AppExceptionHandle = host.Services.GetRequiredService<IAppExceptionHandle>();
            var RcvFromLCU = host.Services.GetRequiredService<IRcvFromLCU>();
            var SendToLCU = host.Services.GetRequiredService<ISendToLCU>();
            var DeleToLCU = host.Services.GetRequiredService<IDeteToLCU>();
            #endregion


            //如果要做到跨Session唯一，名稱可加入"Global\"前綴字
            //如此即使用多個帳號透過Terminal Service登入系統
            //整台機器也只能執行一份
            //appGuidExe = Process.GetCurrentProcess().ProcessName + "exe";
            ////appGuid = appGuidExe + "^" + args[1] + "^" + args[2];
            //appGuid = appGuidExe;



            // 測試參數
            ComQryLCU.Parameter_Url = "172.20.8.235";
            ComQryLCU.Parameter_GUID = "1C078330-69F0-4450-A698-7DA0A1CDDEF4";
            ComQryLCU.Parameter_Value = "1";


            //ComQryLCU.Parameter_Url = args[0];
            //ComQryLCU.Parameter_GUID = args[1];
            //ComQryLCU.Parameter_Value = args[2];


            // 在exe所在的位子，產生3個資料夾
            FileDirectory = AppDomain.CurrentDomain.BaseDirectory + ComQryLCU.Parameter_Url + "\\" + ComQryLCU.Parameter_GUID;
            //FileDirectory = ComQryLCU.Parameter_Url + "\\" + ComQryLCU.Parameter_GUID;
            FileDirectory_SendBackup = FileDirectory + "\\SendDataBackup";
            FileDirectory_ReturnBackup = FileDirectory + "\\RcvDataBackup";
            Directory.CreateDirectory(FileDirectory);
            Directory.CreateDirectory(FileDirectory_SendBackup);
            Directory.CreateDirectory(FileDirectory_ReturnBackup);


            ComQryLCU.Agent_WriteLog("==================================================================================");

            // 意外錯誤處理
            AppExceptionHandle.AppExceptionHandle();

            // FTP 連線
            ComQryLCU.FTP_Connectoin(ref ftpclient);

            // exe起來， 寫入 status1
            // ComQryLCU.UpDateStatus(1);


            // value = 1
            // 回收實績
            if ((int.Parse(ComQryLCU.Parameter_Value) & 1) == 1)
            {
                try
                {
                    ComQryLCU.Parameter_Step = "1";
                    ComQryLCU.Agent_WriteLog(" Value = 1，回收實績");
                    RcvFromLCU.MainFunction("1");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            // value = 2
            // 清除門市實績
            if ((int.Parse(ComQryLCU.Parameter_Value) & 2) == 2)
            {
                try
                {
                    ComQryLCU.Parameter_Step = "2";
                    ComQryLCU.Agent_WriteLog("");
                    ComQryLCU.Agent_WriteLog(" Value = 2，清除門市實績");
                    DeleToLCU.MainFunction("2");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            //// value = 2
            //// 清除實績 店鋪
            //if ((int.Parse(ComQryLCU.Parameter_Value) & 2) == 2)
            //{
            //    try
            //    {
            //        ComQryLCU.Parameter_Step = "2";
            //        DeleToLCU.MainFunction("2");
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex.Message);
            //    }
            //}

            // value = 4
            // 清除主檔
            if ((int.Parse(ComQryLCU.Parameter_Value) & 4) == 4)
            {
                try
                {
                    ComQryLCU.Parameter_Step = "4";
                    ComQryLCU.Agent_WriteLog("");
                    ComQryLCU.Agent_WriteLog(" Value = 4，測試清除主檔");
                    ComQryLCU.Agent_WriteLog(" 測試清除主檔");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }


            // value = 8
            // 清除主檔
            if ((int.Parse(ComQryLCU.Parameter_Value) & 8) == 8)
            {
                try
                {
                    ComQryLCU.Parameter_Step = "8";
                    ComQryLCU.Agent_WriteLog(" ");
                    ComQryLCU.Agent_WriteLog(" Value = 8");
                    ComQryLCU.Agent_WriteLog(" 測試清除主檔");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            // value = 8
            // 清除主檔
            if ((int.Parse(ComQryLCU.Parameter_Value) & 8) == 8)
            {
                try
                {
                    ComQryLCU.Parameter_Step = "8";
                    ComQryLCU.Agent_WriteLog(" 測試");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }


            // value = 16
            // 下載店鋪主檔
            if ((int.Parse(ComQryLCU.Parameter_Value) & 16) == 16)
            {
                try
                {
                    ComQryLCU.Parameter_Step = "16";
                    ComQryLCU.Agent_WriteLog(" ");
                    ComQryLCU.Agent_WriteLog(" Value = 16，下載店鋪主檔");
                    SendToLCU.MainFunction("16");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            // value = 32
            // 下載店鋪主檔
            if ((int.Parse(ComQryLCU.Parameter_Value) & 32) == 32)
            {
                try
                {
                    ComQryLCU.Parameter_Step = "32";
                    ComQryLCU.Agent_WriteLog(" ");
                    ComQryLCU.Agent_WriteLog(" Value = 32，下載店鋪主檔");
                    string result = SendToLCU.MainFunction("32");
                    ComQryLCU.Agent_WriteLog(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            // 下載分揀

            if ((int.Parse(ComQryLCU.Parameter_Value) & 64) == 64)
            {
                try
                {
                    ComQryLCU.Parameter_Step = "64";
                    ComQryLCU.Agent_WriteLog(" ");
                    ComQryLCU.Agent_WriteLog(" Value = 64，下載分揀");
                    string result = SendToLCU.MainFunction("64");
                    ComQryLCU.Agent_WriteLog(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            // 結束
            if ((int.Parse(ComQryLCU.Parameter_Value) & 64) == 64)
            {
                ComQryLCU.Parameter_Step = "64";
                ComQryLCU.Agent_WriteLog(" ");
                ComQryLCU.Agent_WriteLog(" Value = 64，下載分揀");
                string result = SendToLCU.MainFunction("64");
            }



            //結束  寫入 status1
            //ComQryLCU.UpDateStatus(2);

        }
    }
}
