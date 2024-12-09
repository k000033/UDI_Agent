﻿using Agent_ClassLibrary.FtpFileHandle;
using Agent_ClassLibrary.Gloab;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UDI_FTP_TERAKO_Agent.Handel.GetTxtFromTERKO;

namespace UDI_FTP_TERAKO_Agent.Handel.GetTxtFromTERAKO
{
    public class GetTxtFromTERAKO:IGetTxtFromTERAKO
    {

        private readonly IFtpFileHndel _ftpFileHndel;
        private readonly IGlobalUtility _global;

        public GetTxtFromTERAKO(IFtpFileHndel ftpFileHndel,IGlobalUtility global)
        {
            _ftpFileHndel = ftpFileHndel;
            _global = global;
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
                using (StreamReader sr = File.OpenText(GetFilePath))
                {
                    intItems = 0;
                    string input = "";
                    while ((input = sr.ReadLine()) != null)
                    {
                        Hashtable ht_Query2 = new Hashtable();
                        ht_Query2.Add("@GUID", _global.Parameter_GUID);
                        ht_Query2.Add("@IDX", intItems);
                        ht_Query2.Add("@TXT", input);
                        _global.TxtInsertTable(ht_Query2, ref intItems);
                        _global.Agent_WriteLog($"寫入第 {intItems} 筆資料");

                    }

                    _global.Agent_WriteLog($" {GetFileName} {intItems}筆  上傳DB成功 ");
                    _ftpFileHndel.WhriteToResult(spName);
                }
            }
            catch (Exception ex)
            {

                ErrMsg = ex.Message;
            }

            return ErrMsg;
        }
    }
}
