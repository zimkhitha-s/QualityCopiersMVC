using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.SecretManager.V1;

public static class FirebaseInitializer
{
    public static void Initialize()
    {
        if (FirebaseApp.DefaultInstance != null)
            return;

        var projectId = Environment.GetEnvironmentVariable("GCP_PROJECT_ID");
        var secretName = Environment.GetEnvironmentVariable("GCP_SECRET_NAME");

        var client = SecretManagerServiceClient.Create();
        var secretVersion = "latest";
        var secretFullName = $"projects/{projectId}/secrets/{secretName}/versions/{secretVersion}";
        var secret = client.AccessSecretVersion(secretFullName);

        string jsonString = secret.Payload.Data.ToStringUtf8();

        var appOptions = new AppOptions()
        {
            Credential = GoogleCredential.FromJson(jsonString)
        };

        FirebaseApp.Create(appOptions);
        Console.WriteLine("✅ Firebase initialized successfully from Secret Manager.");
    }
}