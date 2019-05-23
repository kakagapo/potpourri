using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Globalization;

namespace CSharpWebHostConsoleApp
{
    class Program
    {
        public static readonly int DefaultPort = 18080;
        public static readonly string DefaultHostName = "localhost";

        static void Main(string[] args)
        {
            string endpointPortSetting = 
                ConfigurationManager.AppSettings.Get("CalcServiceEndpointPort");

            int port = DefaultPort;
            if (!string.IsNullOrEmpty(endpointPortSetting))
            {
                port = int.Parse(endpointPortSetting);
            }

            Uri endpointUri = GetBaseAddress(DefaultHostName, port, Uri.UriSchemeHttp);
            Console.WriteLine("Going to register endpoint : {0}", endpointUri.ToString());

            // Self-hosting the service here. Alternative is to use IIS to host to service.
            var serviceHost = new ServiceHost(typeof(CalculatorService), endpointUri);

            try
            {
                serviceHost.AddServiceEndpoint(typeof(ICalc), new WSHttpBinding(), "CalculatorService");
                
                // Setting the limits
                serviceHost.Description.Behaviors.Add(
                    new ServiceThrottlingBehavior()
                    {
                        MaxConcurrentCalls = Int32.MaxValue,
                        MaxConcurrentSessions = Int32.MaxValue,
                        MaxConcurrentInstances = Int32.MaxValue,
                    });

                // Enable metadata exchange.
                serviceHost.Description.Behaviors.Add(
                    new ServiceMetadataBehavior()
                    {
                        HttpGetEnabled = true,
                    });

                var sab = new ServiceAuthenticationBehavior();
                sab.AuthenticationSchemes = System.Net.AuthenticationSchemes.None;

                serviceHost.Open();
                Console.WriteLine("The service is ready.");

                // Close the ServiceHost to stop the service.
                Console.WriteLine("Press <Enter> to terminate the service.");
                Console.WriteLine();
                Console.ReadLine();
                Console.WriteLine("Shutting down service host.");
                serviceHost.Close();
            }
            catch (CommunicationException ce)
            {
                Console.WriteLine("An exception occurred: {0}", ce.Message);
                serviceHost.Abort();
            }
            catch (Exception e)
            {
                Console.WriteLine("An exception occurred: {0}", e.Message);
                serviceHost.Abort();
            }
            finally
            {
                Console.ReadLine();
            }
        }

        private static Uri GetBaseAddress(string locahostName, int portNumber, string uriScheme)
        {
            UriBuilder uriBuilder = new UriBuilder(
                string.IsNullOrEmpty(uriScheme) ? Uri.UriSchemeHttp : uriScheme,
                locahostName,
                portNumber,
                "root");

            return uriBuilder.Uri;
        }
    }
}
