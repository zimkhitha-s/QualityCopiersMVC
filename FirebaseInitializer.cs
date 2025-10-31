using Google.Cloud.SecretManager.V1;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

namespace INSY7315_ElevateDigitalStudios_POE;

public class FirebaseInitializer
{
    public static void Initialize()
    {
        // Secret Manager client
        var client = SecretManagerServiceClient.Create();
        
        // Replace "my-project-id" and "firebase-key" with your values
        var secretName = "projects/98684336423/secrets/firebase-admin-key/versions/latest";
        var secret = client.AccessSecretVersion(secretName);

        // Get JSON string
        string jsonString = secret.Payload.Data.ToStringUtf8();

        // Initialize Firebase Admin SDK using the JSON from Secret Manager
        var appOptions = new AppOptions()
        {
            Credential = GoogleCredential.FromJson(jsonString)
        };
        if (FirebaseApp.DefaultInstance == null)
            FirebaseApp.Create(appOptions);
    }
}
