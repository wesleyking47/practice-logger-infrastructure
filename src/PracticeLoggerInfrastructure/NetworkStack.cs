using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Constructs;

namespace PracticeLoggerInfrastructure
{
    public class NetworkStack : Stack
    {
        public readonly IVpc Vpc;

        internal NetworkStack(Construct scope, string id, IStackProps props = null)
            : base(scope, id, props)
        {
            this.Vpc = new Vpc(
                this,
                "ApiVpc",
                new VpcProps
                {
                    MaxAzs = 2,
                    NatGateways = 0,
                    SubnetConfiguration = new[]
                    {
                        new SubnetConfiguration
                        {
                            Name = "Public",
                            SubnetType = SubnetType.PUBLIC,
                            CidrMask = 24,
                        },
                        new SubnetConfiguration
                        {
                            Name = "Private",
                            SubnetType = SubnetType.PRIVATE_ISOLATED,
                            CidrMask = 24,
                        },
                    },
                }
            );

            this.Vpc.AddInterfaceEndpoint(
                "SecretsManagerEndpoint",
                new InterfaceVpcEndpointOptions
                {
                    Service = InterfaceVpcEndpointAwsService.SECRETS_MANAGER,
                }
            );

            this.Vpc.AddInterfaceEndpoint(
                "CloudWatchLogsEndpoint",
                new InterfaceVpcEndpointOptions
                {
                    Service = InterfaceVpcEndpointAwsService.CLOUDWATCH_LOGS,
                }
            );

            this.Vpc.AddInterfaceEndpoint(
                "EcrApiEndpoint",
                new InterfaceVpcEndpointOptions
                {
                    Service = InterfaceVpcEndpointAwsService.ECR,
                }
            );

            this.Vpc.AddInterfaceEndpoint(
                "EcrDockerEndpoint",
                new InterfaceVpcEndpointOptions
                {
                    Service = InterfaceVpcEndpointAwsService.ECR_DOCKER,
                }
            );

            this.Vpc.AddInterfaceEndpoint(
                "ApiGatewayEndpoint",
                new InterfaceVpcEndpointOptions
                {
                    Service = InterfaceVpcEndpointAwsService.APIGATEWAY,
                    PrivateDnsEnabled = false,
                }
            );

            this.Vpc.AddGatewayEndpoint(
                "S3Endpoint",
                new GatewayVpcEndpointOptions
                {
                    Service = GatewayVpcEndpointAwsService.S3,
                }
            );
        }
    }
}
