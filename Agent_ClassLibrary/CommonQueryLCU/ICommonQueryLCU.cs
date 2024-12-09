using Agent_ClassLibrary.Ftp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agent_ClassLibrary.CommonQueryLCU
{
    public interface ICommonQueryLCU
    {
        public string Parameter_Url { get; set; }
        public string Parameter_GUID { get; set; }

        public string Parameter_OrderType { get; set; }

        public string Parameter_Step { get; set; }

        public string Parameter_OrderId { get; set; }
        public string Parameter_Value { get; set; }
        public void FTP_Connectoin(ref FtpClient ftpClient);

        public string FTPCheckFileUploadOK(string FileName, ref FtpClient ftp, int ExistType);

        public bool TXTTODB_LCU0301(Hashtable prm, ref int intItems);

        public void Agent_WriteLog(string msg);

        public DataTable GetTxtFromLCU(Hashtable prm);

        public DataSet WhriteToResult();

        public DataSet UpdateState();



        public void Agent_Clean();
      
    }
}
