using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.CDK;

namespace PracticeLoggerInfrastructure
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            var sharedEnv = new Amazon.CDK.Environment
            {
                Account = "231877834621",
                Region = "us-east-2",
            };
            var networkStack = new NetworkStack(app, "NetworkStack", new StackProps { Env = sharedEnv });
            var apiStack = new ApiStack(app, "ApiStack", new ApiStackProps
            {
                Env = sharedEnv,
                Vpc = networkStack.Vpc
            });
            new UiStack(app, "UiStack", new UiStackProps 
            { 
                Env = sharedEnv,
                ApiEndpoint = apiStack.ApiEndpoint,
                Vpc = networkStack.Vpc
            });
            app.Synth();
        }
    }
}
