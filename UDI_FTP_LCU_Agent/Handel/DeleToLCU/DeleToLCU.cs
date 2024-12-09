using Agent_ClassLibrary.CommonQueryLCU;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace UDI_FTP_LCU_Agent.Handel.DeleToLCU
{
    internal class DeleToLCU : IDeteToLCU
    {

        private readonly ICommonQueryLCU _comQryLCU;
        public DeleToLCU(ICommonQueryLCU comQryLCU)
        {
            _comQryLCU = comQryLCU;
        }
        public string MainFunction(string type)
        {
          
            string TXTfile_KeyValue = "";
            string ErrMsg = "";
            string TextName = "";
            try
            {

                switch (type)
                {
                    case "2":
                        //商品
                        TextName = "商品";
                        TXTfile_KeyValue = "HST0121"; break;
                    case "112":
                        //店鋪
                        TextName = "店鋪";
                        TXTfile_KeyValue = "HST0122"; break;
                    case "115":
                        //分揀
                        TextName = "分揀";
                        TXTfile_KeyValue = "HST0123"; break;
                    case "113":
                        //添加物
                        TextName = "添加物";
                        TXTfile_KeyValue = "HST0129"; break;
                    case "114":
                        //班別
                        TextName = "班別";
                        TXTfile_KeyValue = "HST0133"; break;
                    case "120":
                        //實績
                        TextName = "實績";
                        TXTfile_KeyValue = "HST0125"; break;
                }

                string FileName = TXTfile_KeyValue + ".TXT";               //檔案名稱
                string FilePath = Program.FileDirectory + @"\" + FileName;     //輸出文字檔目錄
                
                // 輸出空白的TXT
                #region 根據檔案類別, 輸出空白TXT
                using (StreamWriter sw_OutPutSPACETXT = new StreamWriter(FilePath, false, System.Text.Encoding.Default))
                {
                    sw_OutPutSPACETXT.Write("CS");
                }
                Program.ftpclient.upload(FileName, FilePath);
                _comQryLCU.Agent_WriteLog($" 刪除 {TextName} 實績的" + FileName + " 下傳成功");
                //不會回傳訊息, 給包裝機緩衝時間
                Thread.Sleep(2000);
                #endregion
            }
            catch (Exception ex)
            {
                ErrMsg = ex.Message;
                _comQryLCU.Agent_WriteLog($" {ex.Message}");
                throw;
            }
            return ErrMsg;
        }
    }
}
