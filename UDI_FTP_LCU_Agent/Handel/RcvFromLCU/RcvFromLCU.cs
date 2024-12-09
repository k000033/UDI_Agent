using Agent_ClassLibrary.CommonQueryLCU;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace UDI_FTP_LCU_Agent.Handel.RcvFromLCU
{
    internal class RcvFromLCU : IRcvFromLCU
    {

        private readonly ICommonQueryLCU _comQryLCU;
        public RcvFromLCU(ICommonQueryLCU comQryLCU)
        {
            _comQryLCU = comQryLCU;
        }
        public string MainFunction(string type)
        {
            string FileName = "";
            string FilePath = "";
            string ErrMsg = "";
            DataTable dt_HST0124;
            DataTable dt_LCU0301;       //LCU0301文字檔
            int intItems = 0;


            // SETP 1
            // 清除ftp舊LCU0301.TXT
            #region 清除ftp舊LCU0301.TXT
            FileName = "LCU0301.TXT";
            _comQryLCU.Agent_WriteLog($" {FileName} 開始刪除");
            Program.ftpclient.delete(FileName);
            //_comQryLCU..Step = "LCU0301.TXT";
            _comQryLCU.Agent_WriteLog($" {FileName} 刪除完成");
            #endregion

            // SETP 2
            // 傳送回收檔案的起始編號 HST0124.TXT
            #region 傳送回收檔案的起始編號 HST0124.TXT
            FileName = "HST0124.TXT";
            _comQryLCU.Agent_WriteLog($" {FileName} 開始產生");
            FilePath = Program.FileDirectory + @"\" + FileName;
            // 設定參數
            Hashtable ht_Query = new Hashtable();
            ht_Query.Add("@TASK_ID", _comQryLCU.Parameter_GUID);
            ht_Query.Add("@TYPE", type);
            // 呼叫SP，取得文字檔內容
            DataSet ds_HST0124 = _comQryLCU.GetTxtFromLCU(ht_Query);

            if (ds_HST0124.Tables.Count > 0)
            {
                if (ds_HST0124.Tables[0].Rows.Count > 0)
                {
                    dt_HST0124 = ds_HST0124.Tables[0];
                    // 產生文字檔
                    using (StreamWriter sw_OutPutHST0124 = new StreamWriter(FilePath, false, System.Text.Encoding.Default))
                    {
                        string data = "";
                        foreach (DataRow row in dt_HST0124.Rows)
                        {
                            foreach (DataColumn column in dt_HST0124.Columns)
                            {
                                data = row[column].ToString();
                            }
                            data += "\n";
                            sw_OutPutHST0124.Write(data);
                            data = "";
                        }
                        data += "\n";
                    }
                    // 透過 FTP 給予文字檔
                    Program.ftpclient.upload(FileName, FilePath);
                    _comQryLCU.Agent_WriteLog($" 已在 {_comQryLCU.Parameter_Url} 產生 {FileName}");
                }
                else
                {
                    _comQryLCU.Agent_WriteLog($" {FileName} 沒有資料");
                }
            }

            #endregion

            // SETP 3
            // 通知 設備 產生 實績檔
            #region 通知開始回收 HST0201.TXT
            FileName = "HST0201.TXT";
            FilePath = Program.FileDirectory + @"\" + FileName;
            // 產生文字檔
            using (StreamWriter sw_OutPutHST0201 = new StreamWriter(FilePath, false, Encoding.Default))
            {
                sw_OutPutHST0201.Write("");
            }
            Program.ftpclient.upload(FileName, FilePath);
            #endregion

            // SETP 4
            // 檢查LCU0301.TXT是否已經產生
            #region 檢查LCU0301.TXT是否已經產生
            FileName = "LCU0301.TXT";
            FilePath = Program.FileDirectory + @"\" + FileName;
            ErrMsg = _comQryLCU.FTPCheckFileUploadOK(FileName, ref Program.ftpclient, 0);
            if (ErrMsg != "")
            {
                return ErrMsg;
            }
            #endregion

            // SETP 5
            // 下載LCU0301.TXT
            #region 下載LCU0301.TXT
            string FilePath_ReturnBackup = Program.FileDirectory_ReturnBackup + @"\" + DateTime.Now.ToString("yyyyMMddHHmmss") + FileName;  //備份檔案
            Program.ftpclient.download(FileName, FilePath);
            if (File.Exists(FilePath))
            {
                _comQryLCU.Agent_WriteLog($" {FileName} 下載成功");
            }
            else
            {
                ErrMsg = $" {FileName} 下載失敗";
                _comQryLCU.Agent_WriteLog(ErrMsg);
                return ErrMsg;
            }

            File.Copy(FilePath, FilePath_ReturnBackup);
            #endregion

            // SETP 6
            //  LCU0301.TXT轉為C#_DataTable
            #region LCU0301.TXT轉為C#_DataTable
            DateTime BatchTime_LCU0301 = DateTime.Now;

            using (StreamReader sr = File.OpenText(FilePath))
            {
                intItems = 0;
                string input = "";
                while ((input = sr.ReadLine()) != null)
                {
                    Hashtable ht_Query2 = new Hashtable();
                    ht_Query2.Add("@TASK_ID", _comQryLCU.Parameter_GUID);
                    ht_Query2.Add("@IDX", intItems);
                    ht_Query2.Add("@TXT", input);
                    _comQryLCU.TXTTODB_LCU0301(ht_Query2, ref intItems);

                }
            }

            _comQryLCU.Agent_WriteLog($" 執行了 {intItems} 次");
            _comQryLCU.Agent_WriteLog($" {FileName} 上傳DB成功");
            #endregion
            return "";
        }
    }
}
