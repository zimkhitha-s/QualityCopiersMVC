using Google.Cloud.SecretManager.V1;

namespace INSY7315_ElevateDigitalStudios_POE.Helper
{
    public class SecretManagerHelper
    {
        public static string GetSecret(string projectId, string secretId)
        {
            var client = SecretManagerServiceClient.Create();
            var secretName = $"projects/{projectId}/secrets/{secretId}/versions/latest";
            var secret = client.AccessSecretVersion(secretName);
            return secret.Payload.Data.ToStringUtf8();
        }
    }
}
