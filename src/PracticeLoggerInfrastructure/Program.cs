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
            var apiStack = new ApiStack(app, "ApiStack", new StackProps { Env = sharedEnv });
            new UiStack(app, "UiStack", new UiStackProps 
            { 
                Env = sharedEnv,
                ApiEndpoint = apiStack.ApiEndpoint,
                Vpc = apiStack.Vpc
            });
            app.Synth();
        }
    }
}
