namespace FileMailApi
{
    public static class ConfigurationVariableKeys
    {
        public static string MaxFileSize { get; } = "MAX_FILE_SIZE";
        public static string BucketName { get; } = "BUCKET_NAME";
        public static string UseCORS { get; } = "UseCORS";
        public static string AllowedCORSOrigins { get; } = "AllowedCORSOrigins";
    }
}
