using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CSharpWebHostConsoleApp
{
    [DataContract]
    public enum CalculatorServiceFaultReason
    {
        [EnumMember]
        IJustFeltLikeIt
    }

    [DataContract]
    public class CalculatorServiceFault
    {
    }
}
