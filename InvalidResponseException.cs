using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sensor_tool
{
    public class InvalidResponseException: Exception
    {
        public InvalidResponseException(string p): base(p)
        {
        }
    }
}
