using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using INSY7315_ElevateDigitalStudios_POE.Models;

namespace INSY7315_ElevateDigitalStudios_POE.Services
{
    public class FirebaseService
    {
        private readonly FirestoreDb _firestoreDb;

        public FirebaseService()
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.GetApplicationDefault()
                });
            }

            _firestoreDb = FirestoreDb.Create("insy7315-database");
        }

        public FirestoreDb GetFirestore()
        {
            return _firestoreDb;
        }

        public async Task AddClientAsync(Client client)
        {
            // Navigate to the specific user document
            string userId = "ypqdjnU59xfE6cdE4NoKPAoWPfA2"; 
            DocumentReference userDocRef = _firestoreDb.Collection("users").Document(userId);

            // Navigate to the 'clients' subcollection under that user
            CollectionReference clientsRef = userDocRef.Collection("clients");

            // Add the client document
            await clientsRef.Document(client.id).SetAsync(client);
        }

        public async Task<List<Client>> GetClientsAsync(string userId)
        {
            DocumentReference userDocRef = _firestoreDb.Collection("users").Document(userId);
            CollectionReference clientsRef = userDocRef.Collection("clients");

            QuerySnapshot snapshot = await clientsRef.GetSnapshotAsync();
            var clients = new List<Client>();

            foreach (var doc in snapshot.Documents)
            {
                Client client = doc.ConvertTo<Client>();
                client.id = doc.Id;

                // Handle createdAt safely
                try
                {
                    if (client.createdAt is Timestamp ts)
                    {
                        client.createdAtDateTime = ts.ToDateTime();
                    }
                    else if (client.createdAt is long unixTimestamp)
                    {
                        // Validate timestamp range
                        if (unixTimestamp >= -62135596800 && unixTimestamp <= 253402300799)
                        {
                            client.createdAtDateTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
                        }
                        else
                        {
                            // If out of range, set to a default
                            client.createdAtDateTime = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        // Default fallback if missing or invalid type
                        client.createdAtDateTime = DateTime.UtcNow;
                    }
                }
                catch
                {
                    client.createdAtDateTime = DateTime.UtcNow;
                }

                clients.Add(client);
            }

            return clients;
        }

        public async Task DeleteClientAsync(string userId, string clientId)
        {
            try
            {
                DocumentReference clientDoc = _firestoreDb
                    .Collection("users")
                    .Document(userId)
                    .Collection("clients")
                    .Document(clientId);

                await clientDoc.DeleteAsync();
                Console.WriteLine($"Client {clientId} deleted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting client: {ex.Message}");
            }
        }

        public async Task AddQuotationAsync(Quotation quotation)
        {
            // Correct user ID
            string userId = "vz4maSc0vOgouOGPhtdkFzBlceK2";

            // Reference to user's quotes subcollection
            DocumentReference userDocRef = _firestoreDb.Collection("users").Document(userId);
            CollectionReference quotesRef = userDocRef.Collection("quotes");

            // Generate quote number
            quotation.quoteNumber = $"#{DateTime.Now.Ticks % 100000}";
            quotation.createdAt = Timestamp.FromDateTime(DateTime.UtcNow);

            // Calculate total properly from item data
            double totalAmount = 0;
            if (quotation.Items != null && quotation.Items.Any())
            {
                foreach (var item in quotation.Items)
                {
                    item.amount = item.quantity * item.unitPrice;
                    totalAmount += item.amount;
                }
            }

            // Format the amount for display
            quotation.amount = $"Total: R{totalAmount:0.00}";

            // Add to the 'quotes' subcollection under this user
            await quotesRef.AddAsync(quotation);
        }

        public async Task<List<Quotation>> GetQuotationsAsync()
        {
            try
            {
                string userId = "vz4maSc0vOgouOGPhtdkFzBlceK2"; // user ID
                var quotesRef = _firestoreDb.Collection("users").Document(userId).Collection("quotes");
                var snapshot = await quotesRef.GetSnapshotAsync();

                List<Quotation> quotations = new();

                foreach (var doc in snapshot.Documents)
                {
                    if (doc.Exists)
                    {
                        var quotation = doc.ConvertTo<Quotation>();
                        quotations.Add(quotation);
                    }
                }

                return quotations;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching quotations: {ex.Message}");
                return new List<Quotation>();
            }
        }

        public async Task UpdateQuotationStatusAsync(string userId, string quoteId, string status)
        {
            var quotationDoc = _firestoreDb
                .Collection("users")
                .Document(userId)
                .Collection("quotes")
                .Document(quoteId);

            await quotationDoc.UpdateAsync("status", status);
        }

        public async Task DeleteQuotationAsync(string userId, string quoteId)
        {
            try
            {
                DocumentReference quoteDoc = _firestoreDb
                    .Collection("users")
                    .Document(userId)
                    .Collection("quotes")
                    .Document(quoteId);

                await quoteDoc.DeleteAsync();
                Console.WriteLine($"Quotation {quoteId} deleted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting quotation: {ex.Message}");
            }
        }

    }
}
