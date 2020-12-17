using Pulumi;
using Pulumi.Kubernetes;
using Pulumi.Kubernetes.Types.Inputs.Core.V1;

class AppStack : Stack
{
    public AppStack()
    {
        var config = new Pulumi.Config();
        var redisPort = 6379;

        // getting refererence from AKS Stack
        var cluster = new StackReference($"gutek/aks-infra/{Deployment.Instance.StackName}");
        var kubeConfig = cluster.RequireOutput("KubeConfig").Apply(v => v.ToString());
        var provider = new Provider("k8s", new ProviderArgs { KubeConfig = kubeConfig! });
        var options = new ComponentResourceOptions { Provider = provider };

        var redis = new ServiceDeployment("azure-vote-back", new ServiceDeploymentArgs
        {
            Image = "mcr.microsoft.com/oss/bitnami/redis:6.0.8",
            Ports = {redisPort},
            Env = {new EnvVarArgs { Name = "ALLOW_EMPTY_PASSWORD", Value = "yes"} },
            ServiceType = "ClusterIP"
        }, options);

        var voteapp = new ServiceDeployment("azure-vote-front", new ServiceDeploymentArgs
        {
            Replicas = 3,
            Image = "mcr.microsoft.com/azuredocs/azure-vote-front:v1",
            Ports = {80},
            AllocateIPAddress = true,
            ServiceType = "LoadBalancer",
            Env = {new EnvVarArgs { Name = "REDIS", Value = "azure-vote-back"} },
        }, options);

        this.FrontendIp = voteapp.IpAddress;

    }

    [Output] public Output<string> FrontendIp { get; set; }
}