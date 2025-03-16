using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Ecr.Assets;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.S3.Notifications;
using Constructs;
using System.Collections.Generic;
using System.IO;

namespace Cdk
{
    public class FileMailStack : Stack
    {
        internal FileMailStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            var sourceDir = Path.Combine(Directory.GetCurrentDirectory(), "../../src/FileMail.Backend");
            var targetDir = Path.Combine(Directory.GetCurrentDirectory(), "FileMail.Backend");

            if (Directory.Exists(targetDir))
            {
                Directory.Delete(targetDir, true);
            }
            CopyDirectory(sourceDir, targetDir);

            var vpc = new Amazon.CDK.AWS.EC2.Vpc(this, "MyVpc", new Amazon.CDK.AWS.EC2.VpcProps
            {
                MaxAzs = 2
            });

            var fileUploadBucket = AddBucket();
            AddApi(vpc, targetDir, fileUploadBucket);
            AddLambda(fileUploadBucket);
        }

        private Bucket AddBucket()
        {
            return new Bucket(this, "FileUploadBucket", new BucketProps
            {
                RemovalPolicy = RemovalPolicy.RETAIN,
                BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
                Versioned = true,
            });
        }

        private void AddApi(Vpc vpc, string targetDir, IBucket fileUploadBucket)
        {
            var cluster = new Cluster(this, "FileMailApiCluster", new ClusterProps
            {
                Vpc = vpc,
            });

            var dockerImage = new DockerImageAsset(this, "FileMailApiImage", new DockerImageAssetProps
            {
                Directory = targetDir,
                File = "ApiDockerfile",
                Invalidation = new DockerImageAssetInvalidationOptions
                {
                    BuildArgs = false
                },
            });

            var fargateService = new ApplicationLoadBalancedFargateService(this, "FileMailApi", new ApplicationLoadBalancedFargateServiceProps
            {
                Cluster = cluster,
                DesiredCount = 1,
                TaskImageOptions = new ApplicationLoadBalancedTaskImageOptions
                {
                    Image = ContainerImage.FromDockerImageAsset(dockerImage),
                    ContainerPort = 8080,
                    Environment = new Dictionary<string, string>
                    {
                        { "ASPNETCORE_ENVIRONMENT", "Development" },
                        { "BUCKET_NAME", fileUploadBucket.BucketName }
                    },
                    LogDriver = LogDriver.AwsLogs(new AwsLogDriverProps
                    {
                        StreamPrefix = "FileMailApiApp"
                    })
                },
                MemoryLimitMiB = 1024,
                Cpu = 512,
                AssignPublicIp = true,
                PublicLoadBalancer = true,
                TaskSubnets = new SubnetSelection { SubnetType = SubnetType.PUBLIC },
            });

            fileUploadBucket.GrantPut(fargateService.TaskDefinition.TaskRole);

            var cfnService = fargateService.Service.Node.DefaultChild as CfnService;
            if (cfnService != null)
            {
                cfnService.AddPropertyOverride("NetworkConfiguration.AwsvpcConfiguration.AssignPublicIp", "ENABLED");
            }

            fargateService.TaskDefinition.TaskRole.AddManagedPolicy(
                ManagedPolicy.FromAwsManagedPolicyName("AmazonEC2ContainerRegistryReadOnly"));

            fargateService.TargetGroup.ConfigureHealthCheck(new Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck
            {
                Path = "/health",
                Interval = Duration.Seconds(30),
                Timeout = Duration.Seconds(5),
                HealthyThresholdCount = 2,
                UnhealthyThresholdCount = 3
            });

            fargateService.Service.Connections.SecurityGroups[0].AddIngressRule(
                Peer.AnyIpv4(),
                Port.Tcp(8080),
                "Allow public HTTP access over IPv4"
            );

            fargateService.Service.Connections.SecurityGroups[0].AddIngressRule(
                Peer.AnyIpv6(),
                Port.Tcp(8080),
                "Allow public HTTP access over IPv6"
            );
        }

        private void AddLambda(Bucket fileUploadBucket)
        {
            var lambdaRole = new Role(this, "LambdaExecutionRole", new RoleProps
            {
                AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),
                ManagedPolicies =
                [
                    ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaBasicExecutionRole"),
                    ManagedPolicy.FromAwsManagedPolicyName("AmazonS3ReadOnlyAccess"),
                    ManagedPolicy.FromAwsManagedPolicyName("AmazonSESFullAccess")
                ]
            });

            var buildOption = new BundlingOptions()
            {
                Image = Runtime.DOTNET_8.BundlingImage,
                OutputType = BundlingOutput.ARCHIVED,
                Command =
                [
                   "/bin/sh",
                    "-c",
                    "dotnet tool install -g Amazon.Lambda.Tools && " +
                    "dotnet build && " +
                    "dotnet lambda package --output-package /asset-output/function.zip"
                ]
            };

            var lambdaFunction = new Function(this, "S3FileMonitorLambda", new FunctionProps
            {
                Runtime = Runtime.DOTNET_8,
                MemorySize = 1024,
                Handler = "S3FileMonitorLambda::S3FileMonitorLambda.Function::FunctionHandler",
                Code = Code.FromAsset("FileMail.Backend/S3FileMonitorLambda", new Amazon.CDK.AWS.S3.Assets.AssetOptions
                {
                    Bundling = buildOption
                }),
                Timeout = Duration.Seconds(30),
                Role = lambdaRole,
                Environment = new Dictionary<string, string>
                {
                    { "SENDER_EMAIL", "" },
                },
            });

            fileUploadBucket.GrantRead(lambdaFunction);

            fileUploadBucket.AddEventNotification(EventType.OBJECT_CREATED, new LambdaDestination(lambdaFunction));
        }

        private static void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true);
            }
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                CopyDirectory(dir, Path.Combine(targetDir, Path.GetFileName(dir)));
            }
        }
    }
}