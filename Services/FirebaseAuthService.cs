using System.Text.Json;
using System.Text;

namespace INSY7315_ElevateDigitalStudios_POE.Services
{
    public class FirebaseAuthService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public FirebaseAuthService()
        {
            _apiKey = Environment.GetEnvironmentVariable("FIREBASE_API_KEY");
            _httpClient = new HttpClient();
        }

        public async Task<string> SignInWithEmailPasswordAsync(string email, string password)
        {
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_apiKey}";

            var requestBody = new
            {
                email,
                password,
                returnSecureToken = true
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Firebase login failed:");
                Console.WriteLine(responseString);
                return null;
            }

            var jsonDoc = JsonDocument.Parse(responseString);
            return jsonDoc.RootElement.GetProperty("idToken").GetString();
        }

        public async Task<(bool Success, string ErrorMessage)> ChangePasswordAsync(string email, string currentPassword, string newPassword)
        {
            try
            {
                // Sign in user to get ID token
                var idToken = await SignInWithEmailPasswordAsync(email, currentPassword);

                if (string.IsNullOrEmpty(idToken))
                {
                    return (false, "Invalid email or current password.");
                }

                // Call Firebase accounts:update to change the password
                var url = $"https://identitytoolkit.googleapis.com/v1/accounts:update?key={_apiKey}";
                var requestBody = new
                {
                    idToken,
                    password = newPassword,
                    returnSecureToken = true
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    // parse firebase error if available
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(responseString);
                        if (jsonDoc.RootElement.TryGetProperty("error", out var error))
                        {
                            var message = error.GetProperty("message").GetString();
                            return (false, $"Firebase Error: {message}");
                        }
                    }
                    catch { }
                    return (false, $"Failed to change password. Response: {responseString}");
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"Error changing password: {ex.Message}");
            }
        }

    }
}
//-------------------------------------------------------------------------------------------End Of File--------------------------------------------------------------------//