namespace ERP.Core.Warehouse.Api.Test.Common.Utils
{
    public static class EnvironmentManager
    {
        public static readonly string ApiKey = "integration-test-api-key";

        public static void ApplyEnvironmentAws()
        {
            Environment.SetEnvironmentVariable("S3Storage__AccessKey", "test");
            Environment.SetEnvironmentVariable("S3Storage__SecretKey", "test");
            Environment.SetEnvironmentVariable("S3Storage__Region", "us-east-1");
            Environment.SetEnvironmentVariable("S3Storage__BucketName", "erp-test");
            Environment.SetEnvironmentVariable("S3Storage__ForcePathStyle", "true");
            Environment.SetEnvironmentVariable("S3Storage__PublicKeyBaseUrl", "http://localhost");
            Environment.SetEnvironmentVariable("S3Storage__ServiceUrl", "");

            Environment.SetEnvironmentVariable("AwsSns__AccessKey", "test");
            Environment.SetEnvironmentVariable("AwsSns__SecretKey", "test");
            Environment.SetEnvironmentVariable("AwsSns__Region", "us-east-1");
            Environment.SetEnvironmentVariable("AwsSns__PlatformApplicationArn", "arn:aws:sns:us-east-1:000000000000:app/GCM/test");
            Environment.SetEnvironmentVariable("AwsSns__DefaultTopicArn", "");
        }

        public static void ApplyEnvironmentCorsAndSecurity()
        {
            Environment.SetEnvironmentVariable("Authentication__ApiKey", ApiKey);
            Environment.SetEnvironmentVariable("Cors__AllowedOrigins__0", "http://localhost");
        }
    }
}