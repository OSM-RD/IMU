using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace sensor_tool
{
    [SerializableAttribute]
    [ComVisibleAttribute(true)]
    public class SocketTimeoutException: ApplicationException
    {

        public SocketTimeoutException()
        {
        }

        public SocketTimeoutException(string message): base(message)
        {
        }

        public SocketTimeoutException(string message, Exception inner): base(message, inner)
        {
        }

    }
}
