using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDI_SP_WAS_Agent.Handel.RcvFromWAS
{
    internal interface IRcvFromWAS
    {
        public Task<string> MainFunction(string type);
    }
}
