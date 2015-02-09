using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.IO.Ports;
using log4net;

namespace sensor_tool
{
    public class DeviceConnection:IDisposable
    {
        private readonly static ILog LOG = log4net.LogManager.GetLogger(typeof(DeviceConnection));

        private const byte MAGIC_BYTE = (byte)'*';

        private string _portName;

        public enum CommandType : byte
        {
	        //! Positive reply
	        SEND_ACK = 0,
	        //! Error reply
	        SEND_NACK = 1,

	        /* Device Received payloads */

	        //! Read configuration request
	        READ_CONFIGURATION = 2,
	        //! Write configuration request
	        WRITE_CONFIGURATION = 3,
	        //! Read datetime request
	        READ_DATETIME = 4,
	        //! Write datetime request
	        WRITE_DATETIME = 5,
            //! Battery status
            READ_BATTERY_STATUS = 6,
	        //! Show yourself
	        SHOW_YOURSELF = 7,
	        //! Stop to send data automatically through the BT connection
	        STOP_SEND_DATA = 8,
	        //! Resume the sending data
	        START_SEND_DATA = 9,
	        // Stop to send log data through the BT connection
	        STOP_SEND_LOG = 10,
	        // Start to send log data through the BT connection
	        START_SEND_LOG = 11,
	        // Query for data
	        GET_DATA = 12,
	
	        /* Device Sent payloads */

	        //! Send data
	        SEND_DATA = 50,
	        //! Send log information
	        SEND_LOG = 51,

            UNKNOWN
        }

        public class Response
        {
            private CommandType _type;
            private CommandType _original;
            private int _length;
            private byte[] _data;
            private int _read;

            public Response()
            {
                _type = CommandType.UNKNOWN;
                _original = CommandType.UNKNOWN;
                _length = -1;
                _data = null;
                _read = 0;
            }

            public CommandType Type
            {
                get { return _type; }
                set { _type = value; }
            }

            public bool IsAck
            {
                get
                {
                    return _type == CommandType.SEND_ACK;
                }
            }

            public bool IsNack
            {
                get
                {
                    return _type == CommandType.SEND_NACK;
                }
            }

            public int Length
            {
                get { return _length; }
                set { _length = value; }
            }

            public CommandType Original
            {
                get { return _original; }
                set { _original = value; }
            }


            public byte[] Data
            {
                get { return _data; }
                set { _data = value; }
            }

            public int Read
            {
                get { return _read; }
                set { _read = value; }
            }

        }

        private bool _open;

        private SerialPort _port;

        private Response _response;

        private ISensorCallback _callback;

        public DeviceConnection()
        {
            _port = null;
            _portName = null;
            _open = false;
            _callback = null;
            _response = null;
        }

        public bool IsOpen
        {
            get { return _open; }
            set { _open = value; }
        }

        public ISensorCallback Callback
        {
            get { return _callback; }
            set { _callback = value; }
        }

        private void ClosePort()
        {
            if (_port != null)
            {
                if (_port.IsOpen)
                {
                    try
                    {
                        _port.Close();
                    }
                    catch (IOException e)
                    {
                        // Silence
                    }

                    _port.Dispose();
                    _port = null;
                    _portName = null;
                    _response = null;
                }
            }
        }

        private bool SendCommand(CommandType type, byte[] args) 
        {
            // Discard input data
            _port.DiscardInBuffer();

            // Prepare buffer
            uint argsLength = (uint)(args != null ? args.Length : 0);
            byte[] buffer = new byte[argsLength + 4];
            buffer[0] = MAGIC_BYTE;
            buffer[1] = (byte)type;
            buffer[2] = (byte)((argsLength >> 8) & 0xFF);
            buffer[3] = (byte)(argsLength & 0xFF);

            if (args != null)
            {
                Array.Copy(args, 0, buffer, 4, argsLength);
            }

            try
            {
                _port.Write(buffer, 0, buffer.Length);
            }
            catch (TimeoutException te)
            {
                return false;
            }

            LOG.DebugFormat("Sent command {0}, length {1}. Argument: {2}", type, args != null ? args.Length : 0, args != null ? BitConverter.ToString(args) : "");
            return true;
        }

        private bool TryConnect(string portName)
        {
            // Close port
            ClosePort();

            _portName = portName;

            // Open port
            _port = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One);
            _port.ReadTimeout = 100;
            _port.WriteTimeout = 1000;
            _port.DataReceived += new SerialDataReceivedEventHandler(DataReceived);
            _response = null;

            try
            {
                _port.Open();
            }
            catch (ArgumentException e)
            {
                // Bad port name
                LOG.Debug(string.Format("Bad Port {0}", portName), e);
                return false;
            }
            catch (UnauthorizedAccessException e)
            {
                LOG.Debug(string.Format("Already Open Port {0}", portName), e);
                return false;
            }
            catch (IOException e)
            {
                LOG.Debug(string.Format("Error with Port {0}", portName), e);
                return false;
            }


            // Command success
            return true;
        }

        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            byte[] data = new byte[sp.BytesToRead];
            int read = sp.Read(data, 0, data.Length);

            LOG.DebugFormat("Received Data: {0}", BitConverter.ToString(data));

            if (_response == null)
            {
                _response = new Response();
            }

            int i = 0;
            int discard_datagram = 0;
            while (i < read)
            {
                switch (_response.Read)
                {
                    case 0:
                        //continue;
                        if (data[i] != MAGIC_BYTE)
                        {
                            discard_datagram = 1;
                        }
                        else
                        {
                            discard_datagram = 0;
                        }

                        break;
                    case 1:
                        _response.Type = (CommandType)data[i];
                        break;
                    case 2:
                        _response.Length = data[i] << 8;
                        break;
                    case 3:
                        _response.Length |= data[i];
                        break;
                    case 4:
                        _response.Data = new byte[_response.Length];
                        _response.Data[_response.Read - 4] = data[i];
                        if ((_response.Type == CommandType.SEND_ACK) || (_response.Type == CommandType.SEND_NACK))
                        {
                            _response.Original = (CommandType)data[i];
                        }
                        break;
                    default:
                        _response.Data[_response.Read - 4] = data[i];
                        break;
                }

                _response.Read++;
                i++;
                if (discard_datagram==1)
                {
                    LOG.DebugFormat("Received Datagram with Error {0}, length {1}. Original {2}. Argument: {3}", _response.Type, _response.Length, _response.Original, _response.Data != null ? BitConverter.ToString(_response.Data) : "");
                    _response = null;
                    return;
                }

                if ((_response.Length>0) &&(_response.Read > 3) && ((_response.Read - 4) == _response.Length))
                {
                    LOG.DebugFormat("Received Response {0}, length {1}. Original {2}. Argument: {3}", _response.Type, _response.Length, _response.Original, _response.Data != null ? BitConverter.ToString(_response.Data) : "");

                    byte[] argument = new byte[_response.Length - 1];
                    Array.Copy(_response.Data, 1, argument, 0, argument.Length);

                    switch (_response.Type)
                    {
                        case CommandType.SEND_DATA:
                            _callback.ReceivedData(_response.Data);
                            break;
                        case CommandType.SEND_LOG:
                            _callback.ReceivedLog(_response.Data);
                            break;
                        case CommandType.SEND_ACK:
                            {
                                switch (_response.Original)
                                {
                                    case CommandType.READ_CONFIGURATION:
                                        _callback.ReadConfiguration(argument);
                                        break;
                                    case CommandType.WRITE_CONFIGURATION:
                                        _callback.WriteConfiguration();
                                        break;
                                    case CommandType.READ_DATETIME:
                                        {
                                            int year = (argument[0]<<8) | argument[1];
                                            int month = argument[2];
                                            int day = argument[3];
                                            int hour = argument[4];
                                            int minute = argument[5];
                                            int second = argument[6];

                                            _callback.ReadDateTime(new DateTime(year, month, day, hour, minute, second));
                                        }
                                        break;
                                    case CommandType.WRITE_DATETIME:
                                        {
                                            _callback.WriteDateTime();
                                        }
                                        break;
                                    case CommandType.READ_BATTERY_STATUS:
                                        {
                                            DataStream stream = new DataStream(argument);                                            
                                            _callback.WriteBatteryStatus(stream.ReadUInt32());
                                        }
                                        break;
                                }
                            }
                            break;
                        case CommandType.SEND_NACK:
                            _callback.ReceivedError(_response.Original, argument[0]);
                            break;
                    }

                    _response = new Response();
                } 
            }
        }

        public void Connect(string portName)
        {
            if (TryConnect(portName))
            {
                _open = true;
            }
        }

        public void Close()
        {
            ClosePort();
            _open = false;
        }

        public bool GetDateTime()
        {
            // Send command
            return SendCommand(CommandType.READ_DATETIME, null);
        }

        public bool SetDateTime(DateTime dateTime)
        {
            byte[] data = new byte[8];
            data[0] = (byte)(dateTime.Year >> 8);
            data[1] = (byte)(dateTime.Year & 0xFF);
            data[2] = (byte)dateTime.Month;
            data[3] = (byte)dateTime.Day;
            data[4] = (byte)dateTime.Hour;
            data[5] = (byte)dateTime.Minute;
            data[6] = (byte)dateTime.Second;
            data[7] = (byte)dateTime.DayOfWeek; // day of week

            // Send command
            return SendCommand(CommandType.WRITE_DATETIME, data);
        }

        public bool ReadConfiguration()
        {
            // Send command
            return SendCommand(CommandType.READ_CONFIGURATION, null);
        }

        public bool ReadBatteryStatus()
        {
            return SendCommand(CommandType.READ_BATTERY_STATUS, null);
        }

        public bool WriteConfiguration(byte[] data)
        {
            // Send command
            return SendCommand(CommandType.WRITE_CONFIGURATION, data);
        }

        public bool StopSendData()
        {
            return SendCommand(CommandType.STOP_SEND_DATA, null);
        }

        public bool StartSendData()
        {
            return SendCommand(CommandType.START_SEND_DATA, null);
        }

        public bool ShowYourself()
        {
            return SendCommand(CommandType.SHOW_YOURSELF, null);
        }
        public bool GetSampleData()
        {
            return SendCommand(CommandType.GET_DATA, null);
        }

        public void Dispose()
        {
            ClosePort();
        }


    }
}
