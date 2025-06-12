using Agent_ClassLibrary.Gloab;
using Agent_ClassLibrary.Service;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDI_SP_WAS_Agent.Handel.MainFunction
{
    internal class MainFunction:IMainFunction
    {

        private readonly IDBConn _dBConn;
        private readonly IGlobalUtility _global;


        public MainFunction(IDBConn dBConn, IGlobalUtility global)
        {
            _dBConn = dBConn;
            _global = global;
        }

        public async Task<string> MainFun(string type)
        {
            string ErrMsg = "";
            DataSet dataSet = await _global.Execute();
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
                    _global.LogToDatabase(ErrMsg);
                }
            };
            return ErrMsg;
        }
    }
}
