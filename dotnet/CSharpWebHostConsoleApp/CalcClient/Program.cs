using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalcClient.CalculatorServiceReference;

namespace CalcClient
{
    public class Program
    {
        static void Main(string[] args)
        {
            using (CalculatorServiceClient client = new CalculatorServiceClient())
            {
                // In later versions(>= 7.1) of C# you could just make the Main method async 
                // and convert the following line to use async flavor of the Add method
                var result = client.Add(1, 2);
                Console.WriteLine("Add result : {0}", result);
                Console.ReadLine();
            }
        }
    }
}
