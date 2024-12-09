using Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDI_FTP_ISHIDA_Agent
{
    internal class Class1
    {
        public void DownloadCSV()
        {
            //建立檔名
            string filename = "test.csv";

            //建立內容
            StringBuilder sb = new StringBuilder();
            sb.Append("Name,Height,Weight\n");
            sb.Append("Piggy,167.5,57.5\n");
            sb.Append("Fatty,177.5,82.5\n");
            sb.Append("姓名,身高,體重\n");
            sb.Append("小肥豬,167.5,57.5\n");
            sb.Append("大胖豬,177.5,82.5\n");

            //設定標頭
            Response.AddHeader("Content-disposition", "attachment; filename=\"" + filename + "" + "\"");
            //設定回傳媒體型別(MIME)
            Response.ContentType = "text/csv";
            //設定主體內容編碼
            Response.ContentEncoding = Encoding.UTF8;
            //建立StreamWriter，取得Response的OutputStream並設定編碼為UTF8
            StreamWriter sw = new StreamWriter(Response.OutputStream, Encoding.UTF8);
            //寫入資料
            sw.Write(sb.ToString());
            //關閉StreamWriter
            sw.Close();
            //釋放StreamWriter資源
            sw.Dispose();
            //送出Response
            Response.End();
        }
    }
}
