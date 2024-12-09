using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDI_FTP_LCU_Agent.Handel.SendToLCU
{
    internal interface ISendToLCU
    {
        public string MainFunction(string ORDER_TYPE);
    }
}
