using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sensor_tool
{
    public interface ISensorCallback
    {
        void ReceivedData(byte[] data);

        void ReceivedLog(byte[] log);

        void ReadConfiguration(byte[] configuration);

        void WriteConfiguration();

        void ReadDateTime(DateTime dateTime);

        void WriteDateTime();

        void WriteBatteryStatus(UInt32 batteryStatus);

        void ReceivedError(DeviceConnection.CommandType commandType, int error);
    }
}
