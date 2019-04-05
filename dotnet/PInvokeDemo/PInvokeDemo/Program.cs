using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PInvokeDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            NativeMethods.Beep(1000, 2000);
        }
    }
}
