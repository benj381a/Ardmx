using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;

namespace Ardmx.ASC
{
    public class AscDevice
    {
        public string port { get; private set; }
        public event EventHandler<AscInputEventArgs> InputRecived;
        private SerialPort sp;

        public AscDevice(string port)
        {
            this.port = port;

            sp = new SerialPort(port, 9600);
            sp.Open();

            new Thread(_InputLoop).Start();
        }

        public void Send(AscCommand command)
          => sp.Write(command.command, 0, command.command.Length);

        public void Configure(AscPin pin, AscInput type) => _Configure(pin, 0, (int)type);
        public void Configure(AscPin pin, AscOutput type) => _Configure(pin, 1, (int)type);
        public void Reset() => _Configure(AscPin.ResetConfig, 0, 0);

        public void Value(AscPin pin, int val) => _Value(pin, val);
        public void Value(AscPin pin, AscValue val) => _Value(pin, (int)val);

        public void Set(AscPin pin, int val) => _Value(pin, val);
        public void Set(AscPin pin, AscValue val) => _Value(pin, (int)val);

        private void _Configure(AscPin pin, int IO, int type)
            => new AscCommand(true, pin, (IO << 1) | type).Send(this);

        private void _Value(AscPin pin, int val)
            => new AscCommand(false, pin, val).Send(this);

        private void _InputLoop()
        {
            while (!sp.IsOpen)
                Thread.Sleep(10);

            while (sp.IsOpen)
            {
                Thread.Sleep(10);

                if (sp.BytesToRead == 0)
                    continue;

                int command = sp.ReadByte();

                int pin = (command & 0xF8) >> 3;
                int val = (command & 0x06) >> 1;

                if ((command & 1) == 1) // data continues
                {
                    int data, byteNum = 0;
                    do
                    {
                        data = sp.ReadByte();
                        val |= (data & 0xFE) << ((7 * byteNum) + 1);
                        byteNum++;
                    }
                    while ((data & 1) == 1);
                }

                InputRecived.Invoke(this, new AscInputEventArgs(pin, val));
            }
        }

    }

    public class AscCommand
    {
        public byte[] command;
        public AscCommand(bool configuration, AscPin pin, int updateNumber)
        {
            if (configuration)
            {
                command = new byte[1];
                command[0] |= 0x80;
                command[0] |= (byte)((int)pin << 2);
                command[0] |= (byte)updateNumber;
            }
            else
            {
                if (updateNumber <= 1)
                {
                    command = new byte[1];
                    command[0] |= (byte)((int)pin << 2);
                    command[0] |= (byte)(updateNumber << 1);
                }
                else
                {
                    int numBitsUpdateNumber = (int)Math.Log(updateNumber, 2) + 1;
                    int numBytes = (int)Math.Ceiling((float)(numBitsUpdateNumber - 1) / 7) + 1;

                    command = new byte[numBytes];

                    command[0] |= (byte)((int)pin << 2);
                    command[0] |= (byte)(((updateNumber & 1) > 0 ? 1 : 0) << 1);
                    command[0] |= 1; // data continues

                    for (int i = 1; i < numBytes; i++)
                    {
                        int bitsInPrevBytes = ((i - 1) * 7) + 1;
                        int bitsThisByte = numBitsUpdateNumber - bitsInPrevBytes;
                        if (bitsThisByte > 7)
                            bitsThisByte = 7;

                        for (int j = 0; j < bitsThisByte; j++)
                        {
                            command[i] |= (byte)(((updateNumber & (1 << (j + bitsInPrevBytes))) > 0 ? 1 : 0) << 1 + j);
                        }

                        if (i + 1 == numBytes)
                            continue;

                        command[i] |= 1; // data continues
                    }
                }
            }
        }

        public void Send(AscDevice device)
          => device.Send(this);
    }

    public class AscInputEventArgs : EventArgs
    {
        public AscPin pin;

        public int updateNumber { get; }
        public AscValue value { get; }
        public AscInputEventArgs(int pln, int updateNum)
        {
            pin = (AscPin)pln;

            updateNumber = updateNum;

            if (updateNum == 0 || updateNum == 1)
                value = (AscValue)updateNum;
            else
                value = AscValue.none;
        }
    }

    public enum AscPin
    {
        D0, D1, D2, D3, D4, D5, D6, D7, D8, D9, D10, D11, D12, D13,
        A0, A1, A2, A3, A4, A5,
        DA0, DA1, DA2, DA3, DA4, DA5,
        
        ResetConfig = 0x1F,
    }


    public enum AscInput
    {
        Remove = 0,
        Button = 1,
        Potentiometer = 1,
    }

    public enum AscOutput
    {
        LED = 0,
        IR = 1,
    }

    public enum AscValue
    {
        On = 1, Off = 0,
        HIGH = 1, LOW = 0,
        none = -1,
    }
}
