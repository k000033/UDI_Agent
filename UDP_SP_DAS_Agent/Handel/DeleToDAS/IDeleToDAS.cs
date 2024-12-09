using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDI_SP_DAS_Agent.Handel.DeleToDAS
{
    internal interface IDeleToDAS
    {
        public Task<string> MainFunction(string type);
    }
}
