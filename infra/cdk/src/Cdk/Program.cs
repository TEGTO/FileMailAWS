using Amazon.CDK;

namespace Cdk
{
    static class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            _ = new FileMailStack(app, "FileMailStack", new StackProps());
            app.Synth();
        }
    }
}
