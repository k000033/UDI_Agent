using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDI_SP_WAS_Agent.Handel.SendToWAS
{
    internal interface ISendToWAS
    {
        public Task<string> MainFunction(string type);
    }
}
