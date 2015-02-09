using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace sensor_tool
{
    public class DataStream : MemoryStream
    {
        public DataStream(byte[] data)
            : base(data)
        {
        }

        public DataStream()
            : base()
        {
        }

        public uint ReadUInt32(bool swap = false)
        {
            UInt32 result = 0;
            if (swap)
            {
                result |= (UInt32)ReadByte();
                result |= (UInt32)(ReadByte() << 8);
                result |= (UInt32)(ReadByte() << 16);
                result |= (UInt32)(ReadByte() << 24);              
            }
            else
            {
                result |= (UInt32)(ReadByte() << 24);
                result |= (UInt32)(ReadByte() << 16);
                result |= (UInt32)(ReadByte() << 8);
                result |= (UInt32)ReadByte();
            }
            return result;
        }

        public void WriteUInt32(uint data, bool swap = false)
        {
            if (swap)
            {
                WriteByte((byte)(data & 0xFF));
                WriteByte((byte)((data >> 8) & 0xFF));
                WriteByte((byte)((data >> 16) & 0xFF));
                WriteByte((byte)((data >> 24) & 0xFF));    
            }
            else
            {                
                WriteByte((byte)((data >> 24) & 0xFF));
                WriteByte((byte)((data >> 16) & 0xFF));
                WriteByte((byte)((data >> 8) & 0xFF));
                WriteByte((byte)(data & 0xFF));
            }
        }

        public ushort ReadUInt16()
        {
            UInt16 result = (UInt16)(ReadByte()<<8);
            result |= (UInt16)(ReadByte());
            return result;
        }

        public void WriteUInt16(ushort data)
        {            
            WriteByte((byte)((data >> 8) & 0xFF));
            WriteByte((byte)(data & 0xFF));
        }

        public void ReadBytes(byte[] data)
        {
            for (int i = 0; i < data.Length; ++i)
            {
                data[i] = (byte)ReadByte();
            }
        }

        public void WriteBytes(byte[] data)
        {
            for (int i = 0; i < data.Length; ++i)
            {
                WriteByte(data[i]);
            }
        }

        public DateTime ReadDateTime()
        {
            int year = ReadUInt16();
            int month = ReadByte();
            int day = ReadByte();
            int hour = ReadByte();
            int minute = ReadByte();
            int second = ReadByte();
            // padding
            // byte unused = (byte)ReadByte();

            return new DateTime(year, month, day, hour, minute, second);
        }

        public void WriteDateTime(DateTime dateTime)
        {
            WriteUInt16((ushort)dateTime.Year);
            WriteByte((byte)dateTime.Month);

            WriteByte((byte)dateTime.Day);
            WriteByte((byte)dateTime.Hour);
            WriteByte((byte)dateTime.Minute);
            WriteByte((byte)dateTime.Second);
            // padding
            WriteByte((byte)0);
        }
    }
}
