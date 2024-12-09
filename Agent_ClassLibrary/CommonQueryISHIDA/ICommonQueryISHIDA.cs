using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agent_ClassLibrary.CommonQueryISHIDA
{
    public interface ICommonQueryISHIDA
    {

        public DataTable GetTxtFromISHIDA(Hashtable prm);

        public bool TXTTODB_RFD100(Hashtable prm, ref int intItems);
        public string UploadISHIDA_RFD100();
    }
}
