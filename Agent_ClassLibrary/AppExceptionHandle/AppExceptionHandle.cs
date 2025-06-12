using Agent_ClassLibrary.CommonQueryLCU;
using Agent_ClassLibrary.Gloab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agent_ClassLibrary.AppExceptionHandle
{
    public class AppExceptionHandle : IAppExceptionHandle
    {
        private readonly IGlobalUtility _global;

        public AppExceptionHandle(IGlobalUtility global)
        {
            _global = global;
        }

        void IAppExceptionHandle.AppExceptionHandle()
        {

            // 加載 exe發生意外時，會觸發 Handler 的事件
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            // 加載 exe關閉時，會觸發 Handler 的事件
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
        }

        //非處理UI執行緒錯誤
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception err = e.ExceptionObject as Exception;
            _global.LogToFile(err.Message);
        }

        // 關閉視窗，會觸發的事件
        private void OnProcessExit(object sender, EventArgs e)
        {
            Console.WriteLine("程式結束事件已觸發。");
            // 在這裡你可以執行任何你希望在程式結束時執行的程式碼
        }
    }
}
