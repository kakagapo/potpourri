using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;

namespace CSharpWebHostConsoleApp
{
    [ServiceContract(Name = "CalculatorService")]
    public interface ICalc
    {
        [OperationContract]
        [FaultContract(typeof(CalculatorServiceFault))]
        [WebGet(ResponseFormat = WebMessageFormat.Json, UriTemplate = "add/")]
        int Add(int a, int b);

        [OperationContract]
        [FaultContract(typeof(CalculatorServiceFault))]
        [WebGet(ResponseFormat = WebMessageFormat.Json, UriTemplate = "sub/")]
        int Sub(int a, int b);

    }
}
