using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using INSY7315_ElevateDigitalStudios_POE.Models;
using INSY7315_ElevateDigitalStudios_POE.Models.Dtos;
using INSY7315_ElevateDigitalStudios_POE.Security;

namespace INSY7315_ElevateDigitalStudios_POE.Services
{
    public class FirebaseService
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly EncryptionHelper _encryptionHelper;

        public FirebaseService(EncryptionHelper encryptionHelper)
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.GetApplicationDefault()
                });
            }

            _firestoreDb = FirestoreDb.Create("insy7315-database");
            _encryptionHelper = encryptionHelper;
        }

        public FirestoreDb GetFirestore()
        {
            return _firestoreDb;
        }

        public async Task AddClientAsync(Client client)
        {
            // Encrypt sensitive fields
            client.email = _encryptionHelper.Encrypt(client.email);
            client.phoneNumber = _encryptionHelper.Encrypt(client.phoneNumber);
            client.address = _encryptionHelper.Encrypt(client.address);

            // Navigate to the user document
            string userId = "ypqdjnU59xfE6cdE4NoKPAoWPfA2";
            DocumentReference userDocRef = _firestoreDb.Collection("users").Document(userId);

            // Navigate to the 'clients' subcollection
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

                // Decrypt sensitive fields
                client.email = _encryptionHelper.Decrypt(client.email);
                client.phoneNumber = _encryptionHelper.Decrypt(client.phoneNumber);
                client.address = _encryptionHelper.Decrypt(client.address);

                // Handle createdAt safely
                if (client.createdAt is Timestamp ts)
                    client.createdAtDateTime = ts.ToDateTime();
                else if (client.createdAt is long unixTimestamp)
                    client.createdAtDateTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
                else
                    client.createdAtDateTime = DateTime.UtcNow;

                clients.Add(client);
            }

            return clients;
        }

        // Get a single client by ID
        public async Task<Client> GetClientByIdAsync(string clientId)
        {
            string userId = "ypqdjnU59xfE6cdE4NoKPAoWPfA2";
            DocumentReference clientDocRef = _firestoreDb
                .Collection("users")
                .Document(userId)
                .Collection("clients")
                .Document(clientId);

            DocumentSnapshot snapshot = await clientDocRef.GetSnapshotAsync();
            if (!snapshot.Exists) return null;

            Client client = snapshot.ConvertTo<Client>();
            client.id = snapshot.Id;

            // Decrypt sensitive fields
            client.email = _encryptionHelper.Decrypt(client.email);
            client.phoneNumber = _encryptionHelper.Decrypt(client.phoneNumber);
            client.address = _encryptionHelper.Decrypt(client.address);

            // Handle createdAt safely
            if (client.createdAt is Timestamp ts)
                client.createdAtDateTime = ts.ToDateTime();
            else if (client.createdAt is long unixTimestamp)
                client.createdAtDateTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
            else
                client.createdAtDateTime = DateTime.UtcNow;

            return client;
        }


        public async Task UpdateClientAsync(ClientUpdateDto dto)
        {
            string userId = "ypqdjnU59xfE6cdE4NoKPAoWPfA2";
            DocumentReference clientDocRef = _firestoreDb
                .Collection("users")
                .Document(userId)
                .Collection("clients")
                .Document(dto.Id);

            DocumentSnapshot snapshot = await clientDocRef.GetSnapshotAsync();
            if (!snapshot.Exists) throw new Exception("Client not found");

            Client client = snapshot.ConvertTo<Client>();

            // Split full name into name + surname if provided
            if (!string.IsNullOrWhiteSpace(dto.FullName))
            {
                var nameParts = dto.FullName.Trim().Split(' ', 2);
                client.name = nameParts.Length > 0 ? nameParts[0] : client.name;
                client.surname = nameParts.Length > 1 ? nameParts[1] : client.surname;
            }

            // Update only provided fields
            if (!string.IsNullOrWhiteSpace(dto.CompanyName))
                client.companyName = dto.CompanyName;

            if (!string.IsNullOrWhiteSpace(dto.Email))
                client.email = _encryptionHelper.Encrypt(dto.Email);

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                client.phoneNumber = _encryptionHelper.Encrypt(dto.PhoneNumber);

            if (!string.IsNullOrWhiteSpace(dto.Address))
                client.address = _encryptionHelper.Encrypt(dto.Address);

            await clientDocRef.SetAsync(client, SetOptions.Overwrite);
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

        public async Task<(bool Success, string ErrorMessage)> AddEmployeeAsync(Employee employee)
        {
            try
            {
                // 1️⃣ Create user in Firebase Authentication
                var userRecordArgs = new UserRecordArgs
                {
                    Email = employee.Email,
                    EmailVerified = false,
                    Password = employee.Password,
                    DisplayName = employee.FullName,
                    Disabled = false
                };

                UserRecord newUser = await FirebaseAuth.DefaultInstance.CreateUserAsync(userRecordArgs);

                // 2️⃣ Set UID and encrypt sensitive fields before storing
                employee.Uid = newUser.Uid;

                var encryptedEmployee = new Employee
                {
                    Uid = employee.Uid,
                    IdNumber = employee.IdNumber,
                    Name = _encryptionHelper.Encrypt(employee.Name),
                    Surname = _encryptionHelper.Encrypt(employee.Surname),
                    FullName = _encryptionHelper.Encrypt(employee.FullName),
                    Email = _encryptionHelper.Encrypt(employee.Email),
                    PhoneNumber = _encryptionHelper.Encrypt(employee.PhoneNumber),
                    Role = employee.Role,
                    CreatedAt = employee.CreatedAtDateTime
                };

                // 3️⃣ Save encrypted employee details inside user's subcollection 'employees'
                string userId = "ypqdjnU59xfE6cdE4NoKPAoWPfA2"; // TODO: Replace with logged-in admin UID later
                DocumentReference userDocRef = _firestoreDb.Collection("users").Document(userId);
                CollectionReference employeesRef = userDocRef.Collection("employees");

                await employeesRef.Document(encryptedEmployee.Uid).SetAsync(encryptedEmployee);

                return (true, null);
            }
            catch (FirebaseAuthException ex)
            {
                // Firebase Authentication error (e.g., email already exists)
                return (false, $"Firebase Auth Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                // General error
                return (false, $"Error adding employee: {ex.Message}");
            }
        }

        public async Task<List<Employee>> GetAllEmployeesAsync()
        {
            try
            {
                string userId = "ypqdjnU59xfE6cdE4NoKPAoWPfA2";
                CollectionReference employeesRef = _firestoreDb
                    .Collection("users")
                    .Document(userId)
                    .Collection("employees");

                QuerySnapshot snapshot = await employeesRef.GetSnapshotAsync();

                List<Employee> employees = new List<Employee>();

                foreach (var doc in snapshot.Documents)
                {
                    Employee employee = doc.ConvertTo<Employee>();

                    // Decrypt sensitive fields
                    employee.Name = _encryptionHelper.Decrypt(employee.Name);
                    employee.Surname = _encryptionHelper.Decrypt(employee.Surname);
                    employee.FullName = _encryptionHelper.Decrypt(employee.FullName);
                    employee.Email = _encryptionHelper.Decrypt(employee.Email);
                    employee.PhoneNumber = _encryptionHelper.Decrypt(employee.PhoneNumber);

                    // Convert CreatedAt to CreatedAtDateTime for display
                    if (employee.CreatedAt != null)
                    {
                        if (employee.CreatedAt is Timestamp ts)
                            employee.CreatedAtDateTime = ts.ToDateTime();
                        else if (employee.CreatedAt is DateTime dt)
                            employee.CreatedAtDateTime = dt;
                        else
                            employee.CreatedAtDateTime = DateTime.UtcNow;
                    }

                    employees.Add(employee);
                }

                return employees;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching employees: {ex.Message}");
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
