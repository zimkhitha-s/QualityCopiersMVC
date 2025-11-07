using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.SecretManager.V1;

public static class FirebaseInitializer
{
    public static void Initialize()
    {
        if (FirebaseApp.DefaultInstance != null)
            return;

        var projectIdEnv = Environment.GetEnvironmentVariable("GCP_PROJECT_ID");
        var secretName = Environment.GetEnvironmentVariable("GCP_SECRET_NAME");

        var client = SecretManagerServiceClient.Create();
        var secretVersion = "latest";
        var secretFullName = $"projects/{projectIdEnv}/secrets/{secretName}/versions/{secretVersion}";
        var secret = client.AccessSecretVersion(secretFullName);

        string jsonString = secret.Payload.Data.ToStringUtf8().Replace("\\n", "\n");

        if (jsonString.StartsWith("\""))
        {
            jsonString = System.Text.Json.JsonSerializer.Deserialize<string>(jsonString);
        }

        var credential = GoogleCredential.FromJson(jsonString);

        string projectIdFromJson = (credential.UnderlyingCredential as ServiceAccountCredential)?.ProjectId;

        if (string.IsNullOrEmpty(projectIdFromJson))
        {
            projectIdFromJson = projectIdEnv;
            Console.WriteLine($"Falling back to environment variable for ProjectId: {projectIdFromJson}");
        }

        // Initialize Firebase with explicit ProjectId
        var appOptions = new AppOptions()
        {
            Credential = credential,
            ProjectId = projectIdFromJson
        };

        FirebaseApp.Create(appOptions);
        Console.WriteLine($"Firebase initialized successfully with ProjectId: {projectIdFromJson}");
    }
}
//-------------------------------------------------------------------------------------------End Of File--------------------------------------------------------------------//