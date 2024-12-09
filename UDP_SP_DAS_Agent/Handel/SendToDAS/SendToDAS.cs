using Agent_ClassLibrary.Gloab;
using Agent_ClassLibrary.Service;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDI_SP_DAS_Agent.Handel.SendToDAS
{
    internal class SendToDAS : ISendToDAS
    {
        private readonly IDBConn _dBConn;
        private readonly IGlobalUtility _global;
        public SendToDAS(IDBConn dBConn, IGlobalUtility global)
        {
            _dBConn = dBConn;
            _global = global;
        }
        public async Task<string> MainFunction(string type)
        {
            string ErrMsg = "";
            string guid = _global.Parameter_GUID;
            string spStr = "";


            DataSet dataSet=  await _global.Execute();
            DataTable dataTable = dataSet.Tables[0];

            if (dataTable.Rows.Count > 0)
            {
                DataRow row = dataTable.Rows[0];
                int code = (int)row["RTN_CODE"];


                if (code == 0)
                {
                    await _global.UpdateState();
                }
                else
                {
                    ErrMsg = row["RTN_MESSAGE"].ToString();
                    _global.Wirete_Error(ErrMsg);
                }
            };

            //if (dataSet.Tables.Count == 0)
            //{
            //    return ErrMsg;
            //}
            return ErrMsg;
        }
    }
}
