using Agent_ClassLibrary.Ftp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agent_ClassLibrary.Gloab
{
    public interface IGlobalUtility
    {

        public string Parameter_Url { get; set; }
        public string Parameter_GUID { get; set; }

        public string Parameter_OrderType { get; set; }

        public string Parameter_Step { get; set; }

        public string Parameter_OrderId { get; set; }

        public string Parameter_TaskId {  get; set; }

        public string Parameter_DeviceId { get; set; }

        public void FTP_Connectoin(ref FtpClient ftpClient);

        public string FTPCheckFileUploadOK(string FileName, ref FtpClient ftp, int ExistType);
        public void LogToFile(string msg);

        public void LogToDatabase(string msg);

        public Task<DataSet> Execute();

        public Task<DataSet> UpdateState();
        public void UpdateState1();
        public void Agent_Clean();

        public Task Agent_LocalClean();

        public string HandleType(string type);

        public string HandleName(string type);

        public bool TxtInsertTable(Hashtable prm, ref int intItems);
        public Task<bool> InsertUdiGetWithSqlBulkCopy(DataTable dataTable);

        public void LineNotice(string msg);

    }
}
