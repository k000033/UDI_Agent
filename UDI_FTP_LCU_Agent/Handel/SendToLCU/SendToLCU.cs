using Agent_ClassLibrary.CommonQueryLCU;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace UDI_FTP_LCU_Agent.Handel.SendToLCU
{
    internal class SendToLCU : ISendToLCU
    {
        private readonly ICommonQueryLCU _comQryLCU;
        public SendToLCU(ICommonQueryLCU comQryLCU)
        {
            _comQryLCU = comQryLCU;
        }


        public string MainFunction(string type)
        {
            string ErrMsg = "";

            DataTable dt_FileList = new DataTable("dt_FileList");
            dt_FileList.Columns.Add("KeyValue", typeof(string));
            dt_FileList.Columns.Add("ReturnValue", typeof(string));

            switch (type)
            {
                case "16":
                    //商品
                    dt_FileListAddRow(dt_FileList, "HST0021", "HST0021");
                    break;
                case "32":
                    //店鋪
                    dt_FileListAddRow(dt_FileList, "HST0022", "HST0022");
                    break;
                case "15":
                    //分揀
                    dt_FileListAddRow(dt_FileList, "HST0023", "HST0023");
                    break;
                case "13":
                    //添加物
                    dt_FileListAddRow(dt_FileList, "HST0029", "HST0029");
                    break;
                case "14":
                    //保存方法
                    dt_FileListAddRow(dt_FileList, "HST0031", "HST0031");
                    //班別
                    dt_FileListAddRow(dt_FileList, "HST0033", "HST0033");
                    //托盤
                    dt_FileListAddRow(dt_FileList, "HST0032", "HST0032");
                    //廣告文
                    dt_FileListAddRow(dt_FileList, "HST0030", "HST0030");

                    break;
            }

            foreach (DataRow dr in dt_FileList.Rows)
            {
                ErrMsg = TransTXT(dr[0].ToString(), dr[1].ToString(),type);
            }
            return ErrMsg;
        }

        /// <summary>
        /// 傳檔主程式
        /// 因為工作17 有複數主檔要傳送, 特別改為函數
        /// </summary>
        /// <param name="TXTfile_KeyValue">傳給LCU的檔案名稱</param>
        /// <param name="TXTfile_ReturnValue">LCU回傳的檔案名稱</param>
        /// <param name="GUID">批次</param>
        /// <returns>錯誤訊息</returns>
        private string TransTXT(string TXTfile_KeyValue, string TXTfile_ReturnValue, string type)
        {
            _comQryLCU.Parameter_Step = type;
            _comQryLCU.Agent_WriteLog(" 開始下傳TXT_" + TXTfile_KeyValue);

            string ErrMsg = "";

            //開始下傳TXT, 根據指定的文字檔和裝置
            string FileName = TXTfile_KeyValue + ".TXT"                 //檔案名稱
                , FileNameReturn = TXTfile_KeyValue + ".LCU"
                , FilePath = Program.FileDirectory + @"\" + FileName     //輸出文字檔目錄
                , FilePath_SendBackup = Program.FileDirectory_SendBackup + @"\" + DateTime.Now.ToString("yyyyMMddHHmmss") + FileName   //備份檔案
                , FilePath_RrturnBackup = Program.FileDirectory_ReturnBackup + @"\" + DateTime.Now.ToString("yyyyMMddHHmmss") + FileNameReturn;  //備份檔案

            //HST0023可能會被更改為HST0025
            #region 呼叫sp轉換成TXT
            Hashtable ht_Query = new Hashtable();
            ht_Query.Add("@TASK_ID", _comQryLCU.Parameter_GUID);
            ht_Query.Add("@TYPE", type);
            DataTable dt_Inbound_LCU_TXT = _comQryLCU.GetTxtFromLCU(ht_Query).Tables[0];
           _comQryLCU.Agent_WriteLog(" 取得TXT Count:" + dt_Inbound_LCU_TXT.Rows.Count.ToString());

            FileName = TXTfile_KeyValue + ".TXT";                 //檔案名稱
            FileNameReturn = TXTfile_KeyValue + ".LCU";
            FilePath = Program.FileDirectory + @"\" + FileName;     //輸出文字檔目錄
            #endregion

            #region 有資料, 開始上傳
            if (dt_Inbound_LCU_TXT.Rows.Count > 0)
            {
                #region 輸出指定編號的TXT
                using (StreamWriter sw_OutPutTXT = new StreamWriter(FilePath, false, System.Text.Encoding.Default))
                {
                    string data = "";
                    foreach (DataRow row in dt_Inbound_LCU_TXT.Rows)
                    {
                        foreach (DataColumn column in dt_Inbound_LCU_TXT.Columns)
                        {
                            data = row[column].ToString();
                        }
                        data += "\n";
                        sw_OutPutTXT.Write(data);
                        data = "";
                    }
                    data += "\n";
                }

                //備份上傳資料
                File.Copy(FilePath, FilePath_SendBackup);
                #endregion

                #region 清除ftp舊資料
                Program.ftpclient.delete(FileName);
                _comQryLCU.Agent_WriteLog(" " + FileName + "刪除完成");
                #endregion
    
                #region 下傳TXT.tmp, 確定下傳完成後再更改名稱
                Program.ftpclient.upload(FileName + ".tmp", FilePath);

                ErrMsg = _comQryLCU.FTPCheckFileUploadOK(FileName + ".tmp", ref Program.ftpclient, 0);
                if (ErrMsg != "")
                {
                    return ErrMsg;
                }

                Program.ftpclient.rename(FileName + ".tmp", FileName);

                _comQryLCU.Agent_WriteLog(" " + FileName + "下傳成功");
                #endregion

                #region 檢查檔案是否已經成功上傳,ISHIDA會回傳【結果】
                //FilePath = Program.FileDirectory + @"\" + FileNameReturn;     //輸出文字檔目錄

                ErrMsg = _comQryLCU.FTPCheckFileUploadOK(FileNameReturn, ref Program.ftpclient, 1);
                if (ErrMsg != "")
                {
                    return ErrMsg;
                }
                #endregion
            }
            #endregion

            ErrMsg = "";

            return ErrMsg;
        }



        /// <summary>
        /// 新增Row
        /// </summary>
        /// <param name="dt1"></param>
        /// <param name="keyValue"></param>
        /// <param name="returnValue"></param>
        private void dt_FileListAddRow(DataTable dt1, string keyValue, string returnValue)
        {
            DataRow dr = dt1.NewRow();
            dr[0] = keyValue;
            dr[1] = returnValue;
            dt1.Rows.Add(dr);
        }
    }
}
