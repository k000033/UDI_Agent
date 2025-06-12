using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDI_FTP_TERAKO_Agent.Handel.GetTxtFromTERKO
{
    public interface IGetTxtFromTERAKO
    {
        public Task<string> GetTxtToTable(string GetFilePath);
    }
}
