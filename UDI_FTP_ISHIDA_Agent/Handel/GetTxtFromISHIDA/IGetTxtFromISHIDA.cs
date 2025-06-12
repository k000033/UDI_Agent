using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDI_FTP_ISHIDA_Agent.Handel.GetTxtFromISHIDA
{
    public interface IGetTxtFromISHIDA
    {
        public string GetMessageTxt(string GetFilePath);
        public Task<string> GetTxtToTable(string GetFilePath);
    }
}
