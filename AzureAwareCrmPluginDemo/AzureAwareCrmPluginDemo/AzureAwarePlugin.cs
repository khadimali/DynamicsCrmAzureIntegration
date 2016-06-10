using Microsoft.Xrm.Sdk;
using System;

namespace AzureAwareCrmPluginDemo
{
    public class AzureAwarePlugin : IPlugin
    {
        #region Secure/Unsecure Configuration Setup
        private string _unsecureConfig = null;
        private Guid serviceEndpointId; 

        public AzureAwarePlugin(string unsecureConfig)
        {
            _unsecureConfig = unsecureConfig;

            if (String.IsNullOrEmpty(_unsecureConfig) || !Guid.TryParse(_unsecureConfig, out serviceEndpointId))
            {
                throw new InvalidPluginExecutionException("Service endpoint ID should be passed as config.");
            }
        }
        #endregion
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracer = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = factory.CreateOrganizationService(context.UserId);

            tracer.Trace("Execute > REV: 5");

            try
            {
                Entity entity = (Entity)context.InputParameters["Target"];

                //TODO: Do stuff
                if (entity.LogicalName == "account" && context.MessageName.ToLower() == "create") 
                {
                    int studentId = entity.GetAttributeValue<int>("demo_studentid");

                    if (studentId > 0)
                        PostAccountContextToAzure(serviceProvider, tracer, context, entity);
                    else
                        tracer.Trace("Execute > account.create > Wont post the context to the Service Bus. Student ID not provided.");
                }
            }
            catch (Exception e)
            {
                throw new InvalidPluginExecutionException(e.Message);
            }
        }

        private void PostAccountContextToAzure(IServiceProvider serviceProvider, ITracingService tracer, IPluginExecutionContext context, Entity entity)
        {
            IServiceEndpointNotificationService cloudService = (IServiceEndpointNotificationService)serviceProvider.GetService(typeof(IServiceEndpointNotificationService));
            if (cloudService == null)
                throw new InvalidPluginExecutionException("Failed to retrieve the service bus service.");

            try
            {
                tracer.Trace("Posting the execution context.");

                string response = cloudService.Execute(new EntityReference("serviceendpoint", serviceEndpointId), context);
                
                if (!String.IsNullOrEmpty(response))
                {
                    tracer.Trace("Response = {0}", response);

                    if (response.StartsWith("false,"))
                    {
                        string[] errorInfo = response.Split(',');
                        string exceptionMessage = null;

                        if (errorInfo.Length > 1)
                            exceptionMessage = errorInfo[1];
                        else
                            exceptionMessage = "Unknown error.";

                        throw new InvalidPluginExecutionException(exceptionMessage);
                    }
                    else
                    {
                        string[] studentInfo = response.Split(',');

                        if (studentInfo.Length > 0)
                        {
                            if (entity.Attributes.Contains("name"))
                                entity.Attributes["name"] = studentInfo[0];
                            else
                                entity.Attributes.Add("name", studentInfo[0]);
                        }

                        if (studentInfo.Length > 1) 
                        {
                            if (entity.Attributes.Contains("telephone1"))
                                entity.Attributes["telephone1"] = studentInfo[1];
                            else
                                entity.Attributes.Add("telephone1", studentInfo[1]);
                        }
                    }
                }
                tracer.Trace("Done.");
            }
            catch (Exception e)
            {
                tracer.Trace("Exception: {0}", e.ToString());
                throw;
            }
        }
    }
}
