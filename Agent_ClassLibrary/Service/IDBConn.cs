using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agent_ClassLibrary.Service
{
    public interface IDBConn
    {
        public DataSet SqlQuery(string strDB, string strSql, Hashtable prm, string guid, string type);

        public bool SqlUpdate(string strDB, string strSql, Hashtable prm, ref int counts, string guid, string type);
        public DataSet SqlSp(string strDB, string SpName, Hashtable Prm, string guid, string type);
        public Task<DataSet> asyncSqlSp(string strDB, string SpName, Hashtable Prm, string guid, string type);

        public Task<bool> InsertWithSqlBulkCopy(string strDB, string TableName, DataTable dataTable, string guid, string type);
    }
}
