using System;
using System.Collections.Generic;
using System.Text;

// This namespace is found in the Microsoft.Xrm.Sdk.dll assembly
// found in the SDK\bin folder.
using Microsoft.Xrm.Sdk;

// This namespace is found in Microsoft.ServiceBus.dll assembly 
// found in the Windows Azure SDK 
// Assembly can be found in C:\Program Files\Microsoft SDKs\Azure\.NET SDK\v2.6\ToolsRef if the SDK has been installed via WPI
using Microsoft.ServiceBus;

using System.ServiceModel;
using System.Data.SqlClient;

namespace AzureRelayListener
{
    class Program
    {
        /// <summary>
        /// Creates a two-way endpoint listening for messages from the Windows Azure Service
        /// Bus.
        /// </summary>
        public class TwoWayListener
        {
            /// <summary>
            /// Standard Main() method used by most SDK samples.
            /// </summary>
            static public void Main()
            {
                try
                {
                    ServiceBusEnvironment.SystemConnectivity.Mode = ConnectivityMode.Http;

                    string serviceNamespace = "democrmservicebus";
                    string issuerName = "owner";
                    string issuerKey = "<Your ACS Default Key Here>";
                    string servicePath = "Demo/TwoWay";

                    // Leverage the Azure API to create the correct URI.
                    Uri address = ServiceBusEnvironment.CreateServiceUri(
                        Uri.UriSchemeHttps,
                        serviceNamespace,
                        servicePath);

                    Console.WriteLine("The service address is: " + address);

                    // Using an HTTP binding instead of a SOAP binding for this endpoint.
                    WS2007HttpRelayBinding binding = new WS2007HttpRelayBinding();
                    binding.Security.Mode = EndToEndSecurityMode.Transport;

                    // Create the service host for Azure to post messages to.
                    ServiceHost host = new ServiceHost(typeof(TwoWayEndpoint));
                    host.AddServiceEndpoint(typeof(ITwoWayServiceEndpointPlugin), binding, address);

                    // Create the shared secret credentials object for the endpoint matching the 
                    // Azure access control services issuer 
                    var sharedSecretServiceBusCredential = new TransportClientEndpointBehavior()
                    {
                        TokenProvider = TokenProvider.CreateSharedSecretTokenProvider(issuerName, issuerKey)
                    };

                    // Add the service bus credentials to all endpoints specified in configuration.
                    foreach (var endpoint in host.Description.Endpoints)
                    {
                        endpoint.Behaviors.Add(sharedSecretServiceBusCredential);
                    }

                    // Begin listening for messages posted to Azure.
                    host.Open();

                    Console.WriteLine(Environment.NewLine + "Listening for messages from Azure" +
                        Environment.NewLine + "Press [Enter] to exit");

                    // Keep the listener open until Enter is pressed.
                    Console.ReadLine();

                    Console.Write("Closing the service host...");
                    host.Close();
                    Console.WriteLine(" done.");
                }
                catch (FaultException<ServiceEndpointFault> ex)
                {
                    Console.WriteLine("The application terminated with an error.");
                    Console.WriteLine("Message: {0}", ex.Detail.Message);
                    Console.WriteLine("Inner Fault: {0}",
                        null == ex.InnerException.Message ? "No Inner Fault" : "Has Inner Fault");
                }
                catch (System.TimeoutException ex)
                {
                    Console.WriteLine("The application terminated with an error.");
                    Console.WriteLine("Message: {0}", ex.Message);
                    Console.WriteLine("Stack Trace: {0}", ex.StackTrace);
                    Console.WriteLine("Inner Fault: {0}",
                        null == ex.InnerException.Message ? "No Inner Fault" : ex.InnerException.Message);
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine("The application terminated with an error.");
                    Console.WriteLine(ex.Message);

                    // Display the details of the inner exception.
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine(ex.InnerException.Message);

                        FaultException<ServiceEndpointFault> fe = ex.InnerException
                            as FaultException<ServiceEndpointFault>;
                        if (fe != null)
                        {
                            Console.WriteLine("Message: {0}", fe.Detail.Message);
                            Console.WriteLine("Inner Fault: {0}", null == ex.InnerException.Message ? "No Inner Fault" : "Has Inner Fault");
                        }
                    }
                }

                finally
                {
                    Console.WriteLine("Press <Enter> to exit.");
                    Console.ReadLine();
                }
            }
        }

        #region How-To Sample Code

        /// <summary>
        /// The Execute method is called when a message is posted to the Azure Service
        /// Bus.
        /// </summary>
        [ServiceBehavior]
        private class TwoWayEndpoint : ITwoWayServiceEndpointPlugin
        {
            #region ITwoWayServiceEndpointPlugin Member

            /// <summary>
            /// This method is called when a message is posted to the Azure Service Bus.
            /// </summary>
            /// <param name="context">Data for the request.</param>
            /// <returns>A string of information to be returned to the CRM Online Azure-aware plugin.</returns>
            public string Execute(RemoteExecutionContext context)
            {
                string studentId = null;
                string returnString = null;

                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                {
                    var entity = context.InputParameters["Target"] as Entity;
                    if (entity.Attributes.Contains("demo_studentid"))
                    {
                        studentId = entity.Attributes["demo_studentid"].ToString();
                    }
                }

                if (studentId != null)
                {
                    returnString = GetStudentInfo(studentId);
                }
                else
                {
                    returnString = "false,Student ID not provided.";
                }

                return returnString;
            }

            #endregion

            /// <summary>
            /// This method will extract student information from the LOB on-premises application's database directly
            /// </summary>
            /// <param name="studentId"></param>
            /// <returns></returns>
            string GetStudentInfo(string studentId)
            {
                string returnString = null;

                SqlConnection connection = new SqlConnection("Data Source=<ServerName>Se;Initial Catalog=TestDb;Integrated Security=True");
                using (connection)
                {
                    SqlCommand command = new SqlCommand(
                      "SELECT Name, PhoneNumber FROM Student where studentid = " + studentId, connection);
                    connection.Open();

                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            Console.WriteLine("{0}\t{1}", reader.GetString(0),
                                reader.GetString(1));

                            var name = reader.GetString(0);
                            var phone = reader.GetString(1);

                            returnString = name + "," + phone;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Student ID not found in student database.");
                        returnString = "false,Student ID not found in student database.";
                    }
                    reader.Close();
                }

                return returnString;
            }
        }

        #endregion How-To Sample Code

    }
}
