using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.RDS;
using Amazon.CDK.AwsApigatewayv2Integrations;
using Constructs;

namespace PracticeLoggerInfrastructure
{
    public class ApiStackProps : StackProps
    {
        public IVpc Vpc { get; set; }
    }

    public class ApiStack : Stack
    {
        public readonly string ApiEndpoint;
        public readonly IVpc Vpc;

        internal ApiStack(Construct scope, string id, ApiStackProps props)
            : base(scope, id, props)
        {
            this.Vpc = props.Vpc;

            var dbInstance = new DatabaseInstance(
                this,
                "DbInstance",
                new DatabaseInstanceProps
                {
                    Engine = DatabaseInstanceEngine.Postgres(
                        new PostgresInstanceEngineProps { Version = PostgresEngineVersion.VER_16 }
                    ),
                    Vpc = this.Vpc,
                    VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_ISOLATED },
                    InstanceType = Amazon.CDK.AWS.EC2.InstanceType.Of(
                        InstanceClass.BURSTABLE3,
                        InstanceSize.MICRO
                    ),
                    AllocatedStorage = 20,
                    DatabaseName = "PracticeLoggerDb",
                    Credentials = Credentials.FromGeneratedSecret("dbadmin"),
                    RemovalPolicy = RemovalPolicy.DESTROY,
                    DeleteAutomatedBackups = true,
                }
            );

            // 4. Define the Lambda function in Private Subnet
            var apiFunction = new Function(
                this,
                "ApiFunction",
                new FunctionProps
                {
                    Runtime = Runtime.DOTNET_10, // Must match the target runtime
                    Handler = "PracticeLogger.Api",
                    // Placeholder code. The API pipeline will update this with the real .NET binaries.
                    Code = Code.FromAsset("lambda-placeholder"), 
                    Timeout = Duration.Seconds(30),
                    Vpc = this.Vpc,
                    VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_ISOLATED },
                    Environment = new Dictionary<string, string>
                    {
                        { "ConnectionStrings__PracticeLoggerDb", $"Host={dbInstance.DbInstanceEndpointAddress};Port=5432;Database=PracticeLoggerDb;Username=dbadmin;Password={dbInstance.Secret.SecretValueFromJson("password").UnsafeUnwrap()}" }
                    }
                }
            );

            dbInstance.Connections.AllowDefaultPortFrom(apiFunction);

            var httpApi = new HttpApi(
                this,
                "Api",
                new HttpApiProps
                {
                    DefaultIntegration = new HttpLambdaIntegration("ApiIntegration", apiFunction),
                }
            );

            this.ApiEndpoint = httpApi.ApiEndpoint;

            new CfnOutput(
                this,
                "ApiEndpoint",
                new CfnOutputProps
                {
                    Value = httpApi.ApiEndpoint,
                    Description = "HTTP API endpoint for the Practice Logger API",
                }
            );

            new CfnOutput(
                this,
                "ApiFunctionName",
                new CfnOutputProps
                {
                    Value = apiFunction.FunctionName,
                    Description = "Lambda Function Name for API deployment",
                }
            );
        }
    }
}
