using Agent_ClassLibrary.FtpFileHandle;
using Agent_ClassLibrary.Gloab;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDI_FTP_ISHIDA_Agent.Handel.GetTxtFromISHIDA
{
    public class GetTxtFromISHIDA : IGetTxtFromISHIDA
    {

        private readonly IGlobalUtility _global;
        private readonly IFtpFileHndel _ftpFileHndel;

        public GetTxtFromISHIDA(IGlobalUtility global, IFtpFileHndel ftpFileHndel)
        {
            _global = global;
            _ftpFileHndel = ftpFileHndel;
        }

        public string GetMessageTxt(string GetFilePath)
        {
            string GetFileName = _ftpFileHndel.GetFileName.Split("_")[0];
            string AllData = "";
            string ErrMsg = "";
            try
            {
                //解析【ISHIDA回傳結果】是否正常
                using (StreamReader s = new StreamReader(GetFilePath, System.Text.Encoding.UTF8))
                {
                    AllData = s.ReadToEnd();
                }
                string[] rows = AllData.Split("\n".ToCharArray());


                foreach (string r in rows)
                {
                    string[] items = r.Split(',');
                    //第一行是欄位名稱
                    //第二行裁示回傳結果
                    if (items[0] == GetFileName)
                    {
                        //異常
                        if (items[1] == "9")
                        {
                            string MSG = items[3];
                            MSG = MSG.Substring(MSG.IndexOf("(") + 1, MSG.IndexOf(")") - MSG.IndexOf("(") - 1);
                            switch (MSG)
                            {
                                case "400040":  //已經生產完成
                                    break;
                                case "500030":  //找不到要刪除的資料
                                    break;
                                case "500050":  //要登錄的資料已有登錄了
                                    break;
                                case "500060":  //主檔中未登錄
                                    break;
                                default:
                                    ErrMsg += "ERROR: Line:" + items[2] + " Msg:" + items[3] + "\n";
                                    break;
                            }
                            continue;
                        }
                        _global.Agent_WriteLog(" 成功更新筆數" + items[4]);
                    }
                }
            }
            catch (Exception ex)
            {

                ErrMsg = ex.Message;
            }

            return ErrMsg;

        }


        public string GetTxtToTable(string GetFilePath)
        {

            string GetFileName = _ftpFileHndel.GetFileName;
            string ErrMsg = "";
            string AllData = "";
            string spName = _ftpFileHndel.PostGet;
            int intItems = 0;
            try
            {
                using (StreamReader sr = new StreamReader(GetFilePath, Encoding.UTF8))
                {
                    AllData = sr.ReadToEnd();
                }

                AllData = AllData.Replace("\r", "");   //回收檔案有\r, 過濾掉
                string[] rows = AllData.Split("\n".ToCharArray());

                // 去除陣列元素是空字串
                rows = rows.Except(new string[] { "" }).ToArray();

                // 從第二行開始取得資料
                for (int row_Number = 1; row_Number < rows.Length; row_Number++)
                {
                    Hashtable ht_Query2 = new Hashtable();
                    ht_Query2.Add("@GUID", _global.Parameter_GUID);
                    ht_Query2.Add("@IDX", intItems);
                    ht_Query2.Add("@TXT", rows[row_Number]);
                    _global.TxtInsertTable(ht_Query2, ref intItems);
                    _global.Agent_WriteLog($"寫入第 {row_Number} 筆資料");
                }

                _global.Agent_WriteLog($" {GetFileName} {intItems}筆  上傳DB成功 ");
                _global.Agent_WriteLog($" {spName} ");
                _ftpFileHndel.WhriteToResult(spName);
            }
            catch (Exception ex)
            {

                ErrMsg = ex.Message;
            }

            return ErrMsg;
        }

    }
}
