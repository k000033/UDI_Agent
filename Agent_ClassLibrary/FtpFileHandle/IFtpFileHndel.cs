using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Agent_ClassLibrary.FtpFileHandle
{
    public interface IFtpFileHndel
    {
        //PutFileName = dsFtpOrder.Tables[0].Rows[0]["PUT_FILENAME"].ToString();
        //GetFileName = dsFtpOrder.Tables[0].Rows[0]["GET_FILENAME"].ToString();
        //PostGet

        public string PutFileName { get; set; }
        public string GetFileName { get; set; }
        
        public string PostGet { get; set; }

        public DataSet GetFtpOrder(Hashtable prm);
        public Task<DataSet> GetFtpPutTxt(Hashtable prm);

        public DataSet WhriteToResult(string SpName);


        public Task<string> ExecuteAction();
        public Task<string> GetftpOrder();
    }
}
