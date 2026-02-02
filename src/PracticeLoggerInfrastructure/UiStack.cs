using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.SSM;
using Constructs;
using System.Collections.Generic;

namespace PracticeLoggerInfrastructure
{
    public class UiStackProps : StackProps
    {
        public string ApiEndpoint { get; set; }
        public IVpc Vpc { get; set; }
    }

    public class UiStack : Stack
    {
        internal UiStack(Construct scope, string id, UiStackProps props)
            : base(scope, id, props)
        {
            // 1. Create ECR Repository for the UI image
            var repository = new Repository(this, "UiRepository", new RepositoryProps
            {
                RepositoryName = "practice-logger-ui",
                RemovalPolicy = RemovalPolicy.DESTROY, // For practice/dev. Use RETAIN for prod.
                EmptyOnDelete = true
            });

            // 2. Create Fargate Service using image from ECR
            var sessionSecret = StringParameter.FromSecureStringParameterAttributes(this, "UiSessionSecret", new SecureStringParameterAttributes
            {
                ParameterName = "/practice-logger/ui/session-secret"
            });

            var service = new ApplicationLoadBalancedFargateService(this, "UiService", new ApplicationLoadBalancedFargateServiceProps
            {
                Vpc = props.Vpc,
                AssignPublicIp = true,
                TaskSubnets = new SubnetSelection { SubnetType = SubnetType.PUBLIC },
                TaskImageOptions = new ApplicationLoadBalancedTaskImageOptions
                {
                    Image = ContainerImage.FromEcrRepository(repository, "latest"),
                    ContainerPort = 3000,
                    Environment = new Dictionary<string, string>
                    {
                        { "VITE_API_URL", props.ApiEndpoint },
                        { "HOST", "0.0.0.0" },
                        { "PORT", "3000" }
                    },
                    Secrets = new Dictionary<string, Secret>
                    {
                        { "SESSION_SECRET", Secret.FromSsmParameter(sessionSecret) }
                    }
                }
            });

            service.TargetGroup.ConfigureHealthCheck(new Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck
            {
                Path = "/login",
                HealthyHttpCodes = "200"
            });

            // 3. Outputs for CI/CD pipelines
            new CfnOutput(this, "UiRepositoryUri", new CfnOutputProps 
            { 
                Value = repository.RepositoryUri,
                Description = "ECR Repository URI for UI" 
            });

            new CfnOutput(this, "UiClusterName", new CfnOutputProps 
            { 
                Value = service.Cluster.ClusterName,
                Description = "ECS Cluster Name" 
            });

            new CfnOutput(this, "UiServiceName", new CfnOutputProps 
            { 
                Value = service.Service.ServiceName,
                Description = "ECS Service Name" 
            });
        }
    }
}
