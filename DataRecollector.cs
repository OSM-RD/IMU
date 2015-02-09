using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace sensor_tool
{
    class DataRecollector:IDisposable
    {
        private static StreamWriter stream = null;

        public DataRecollector(String fileName)
        {
            File.Delete(fileName);
            stream = File.AppendText(fileName);
            stream.AutoFlush = true;
        }

        public void recollectData(String data)
        {
            DateTime date = DateTime.Now;
            stream.WriteLine(date.ToString("dd/MM/yyyy HH:mm:ss") + "\t" + data);
        }

        public void Dispose()
        {
            stream.Close();
            stream.Dispose();
        }
    }
}
