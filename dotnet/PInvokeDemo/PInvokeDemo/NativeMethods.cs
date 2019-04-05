using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;

namespace PInvokeDemo
{
    class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool Beep(uint freq, uint dur);

        // Todo: Add something that requires explicit marshalling as well.
    }
}
