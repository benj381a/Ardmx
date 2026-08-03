using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Ardmx.ASC;
using Ardmx.Art_Net;
using Ardmx.UI;

namespace Ardmx.ardmx_controller
{
    internal class Program
    {
        static void Main(string[] args)
        {
            AscDevice device = new AscDevice("COM3");
            device.Configure(AscPin.D7, AscOutput.IR);
        }
    }
}
