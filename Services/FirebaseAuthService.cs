using System.Text.Json;
using System.Text;

namespace INSY7315_ElevateDigitalStudios_POE.Services
{
    public class FirebaseAuthService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public FirebaseAuthService(IConfiguration configuration)
        {
            _apiKey = configuration["Firebase:ApiKey"];
            _httpClient = new HttpClient();
        }

        public async Task<string> SignInWithEmailPasswordAsync(string email, string password)
        {
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_apiKey}";

            var requestBody = new
            {
                email = email,
                password = password,
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

    }
}
//-------------------------------------------------------------------------------------------End Of File--------------------------------------------------------------------//