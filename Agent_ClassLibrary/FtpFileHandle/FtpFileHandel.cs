using Agent_ClassLibrary.Gloab;
using Agent_ClassLibrary.Service;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agent_ClassLibrary.FtpFileHandle
{
    public class FtpFileHandel : IFtpFileHndel
    {
        private readonly IDBConn _dbConn;
        private readonly IGlobalUtility _global;

        public FtpFileHandel(IDBConn dbConn, IGlobalUtility global)
        {
            _dbConn = dbConn;
            _global = global;
        }


        private string _PutFileName;
        private string _GetFileName;
        private string _PostGet;

        public string PutFileName { get => _PutFileName; set => _PutFileName = value; }
        public string GetFileName { get => _GetFileName; set => _GetFileName = value; }
        public string PostGet { get => _PostGet; set => _PostGet = value; }

        public DataSet GetFtpOrder(Hashtable prm)
        {
            DataSet dataset = new DataSet();
            string sqlString = @"SELECT PUT_FILENAME 
                                       ,GET_FILENAME
                                       ,POST_GET
                                   FROM [ftp].[UDI_ORDER]
                                  WHERE GUID = @GUID";

            dataset = _dbConn.SqlQuery("UDI", sqlString, prm, _global.Parameter_GUID, _global.Parameter_Step);
            return dataset;
        }

        public async Task<DataSet> GetFtpPutTxt(Hashtable prm)
        {
            DataSet dataset = new DataSet();
            string sqlString = @"SELECT TXT
                                   FROM [ftp].[UDI_PUT]
                                  WHERE GUID = @GUID
                                  ORDER BY IDX";
                              


            dataset = await Task.Run(()=>_dbConn.SqlQuery("UDI", sqlString, prm, _global.Parameter_GUID, _global.Parameter_Step));
            return dataset;
        }

        public DataSet WhriteToResult(string SpName)
        {
            string strSp = $@"{SpName}";
            Hashtable ht_Query = new Hashtable();
            ht_Query.Add("GUID", _global.Parameter_GUID);
            DataSet dt = _dbConn.SqlSp("UDI", strSp, ht_Query, _global.Parameter_GUID, _global.Parameter_Step);
            return dt;
        }


        public async Task<string> ExecuteAction()
        {
            string ErrMsg = "";
            try
            {
                DataSet dataSet = await _global.Execute();
                DataTable dataTable = dataSet.Tables[0];
                if (dataTable.Rows.Count > 0)
                {
                    DataRow row = dataTable.Rows[0];
                    int code = (int)row["RTN_CODE"];
                    if (code != 0)
                    {
                        ErrMsg = row["RTN_MESSAGE"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {

                ErrMsg = ex.Message;
            }
            return ErrMsg;
        }


        public async Task<string> GetftpOrder()
        {
            string ErrMsg = "";
            try
            {
                Hashtable hashtable = new Hashtable();
                hashtable.Add("GUID", _global.Parameter_GUID);
                DataSet dsFtpOrder = await Task.Run(() => GetFtpOrder(hashtable));
                if (dsFtpOrder.Tables.Count > 0)
                {
                    if (dsFtpOrder.Tables[0].Rows.Count > 0)
                    {
                        PutFileName = dsFtpOrder.Tables[0].Rows[0]["PUT_FILENAME"].ToString();
                        GetFileName = dsFtpOrder.Tables[0].Rows[0]["GET_FILENAME"].ToString();
                        PostGet = dsFtpOrder.Tables[0].Rows[0]["POST_GET"].ToString();
                    }
                }
                else
                {
                    ErrMsg = "取不到文件名稱";
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
