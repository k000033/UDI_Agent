using Agent_ClassLibrary.CommonQueryLCU;
using Agent_ClassLibrary.Gloab;
using Agent_ClassLibrary.Service;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agent_ClassLibrary.CommonQueryISHIDA
{
    public class CommonQueryISHIDA : ICommonQueryISHIDA
    {
        private readonly IGlobalUtility _global;
        private readonly IDBConn _dBConn;
        public CommonQueryISHIDA(IGlobalUtility global, IDBConn dBConn)
        {
            _global = global;
            _dBConn = dBConn;
        }


        public DataTable GetTxtFromISHIDA(Hashtable prm)
        {
            DataTable dataTable = new DataTable();
            try
            {
                string strSql = @"SELECT TXT
                                FROM [ftp].[UDI_PUT]
                               WHERE GUID = GUID
                               ORDER BY IDX ";

                 dataTable = _dBConn.SqlQuery("UDI", strSql, prm, _global.Parameter_GUID, _global.Parameter_Step).Tables[0];
               
            }
            catch (Exception ex)
            {

                _global.Wirete_Error(ex.Message);
            }
            return dataTable;
        }


        public bool TXTTODB_RFD100(Hashtable prm, ref int intItems)
        {
            bool result = false;
            try
            {
                string strSql = @"INSERT [ftp].[UDI_GET] (TASK_ID,IDX,TXT,CRT_TIME)
                              SELECT @TASK_ID
                             ,@IDX
	                         ,@TXT
	                         ,GETDATE()";
                result = _dBConn.SqlUpdate("UDI", strSql, prm, ref intItems, _global.Parameter_GUID, _global.Parameter_Step);
             
            }
            catch (Exception ex)
            {

                _global.Wirete_Error(ex.Message);
            }

            return result;
        }

        public string UploadISHIDA_RFD100()
        {
            throw new NotImplementedException();
        }


    }
}
