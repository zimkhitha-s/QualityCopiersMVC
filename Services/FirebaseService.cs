using System.Drawing;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using INSY7315_ElevateDigitalStudios_POE.Models;
using INSY7315_ElevateDigitalStudios_POE.Models.Dtos;
using INSY7315_ElevateDigitalStudios_POE.Security;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Action;
using MimeKit;
using Spire.Doc;
using Spire.Doc.Documents;
using Spire.Doc.Fields;




namespace INSY7315_ElevateDigitalStudios_POE.Services
{
    public class FirebaseService
    {
        // setting up firestore connection and encryption helper
        private readonly FirestoreDb _firestoreDb;
        private readonly EncryptionHelper _encryptionHelper;
        private readonly MailService _mailService;
        private readonly IConfiguration _configuration;

        private readonly string _senderEmail;
        private readonly string _smtpPassword;

        // firebase service constructor - initialize firestore connection and encryption helper
        public FirebaseService(EncryptionHelper encryptionHelper, MailService mailService, IConfiguration configuration)
        {
            // check if firebase app is already initialized
            if (FirebaseApp.DefaultInstance == null)
            {
                // initialize firebase app with default credentials
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromFile("database/firebase-key.json")
                });
            }

            // initialize firestore database and encryption helper
            _firestoreDb = FirestoreDb.Create("insy7315-database2");
            _encryptionHelper = encryptionHelper;
            _mailService = mailService;
            _configuration = configuration;

            _senderEmail = _configuration["EmailSettings:SmtpUser"];
            _smtpPassword = _configuration["EmailSettings:SmtpPassword"];
        }

        // method to get firestore database instance
        public FirestoreDb GetFirestore()
        {
            return _firestoreDb;
        }

        public async Task<string> GetUserRoleAsync(string userId)
        {
            string documentRef = "daMmNRUlirZSsh4zC1c3N7AtqCG2";
            // Check Employees (Admins)
            var employeeDoc = await _firestoreDb.Collection("users")
            .Document(documentRef)
            .Collection("employees")
            .Document(userId)
            .GetSnapshotAsync();

            if (employeeDoc.Exists)
                return employeeDoc.GetValue<string>("role"); // "admin"

            // Check Managers
            var managerDoc = await _firestoreDb.Collection("users")
            .Document(documentRef)
            .Collection("manager_data")
            .Document(userId)
            .GetSnapshotAsync();

            if (managerDoc.Exists)
                return managerDoc.GetValue<string>("role"); // "manager"

            return null;
        }

        // method to add a new client to firestore
        public async Task AddClientAsync(Client client)
        {
            // Assign unique ID
            client.id = Guid.NewGuid().ToString();
            // encrypting the sensitive fields
            client.name = _encryptionHelper.Encrypt(client.name);
            client.surname = _encryptionHelper.Encrypt(client.surname);
            client.email = _encryptionHelper.Encrypt(client.email);
            client.phoneNumber = _encryptionHelper.Encrypt(client.phoneNumber);
            client.address = _encryptionHelper.Encrypt(client.address);
            client.companyName = _encryptionHelper.Encrypt(client.companyName);

            var clientsRef = GetClientsCollection();

            // adding the client document
            await clientsRef.Document(client.id).SetAsync(client);
        }

        // method to get all clients for a user
        public async Task<List<Client>> GetClientsAsync()
        {
            var clientsRef = GetClientsCollection();

            QuerySnapshot snapshot = await clientsRef.GetSnapshotAsync();
            var clients = new List<Client>();

            foreach (var doc in snapshot.Documents)
            {
                Client client = doc.ConvertTo<Client>();
                client.id = doc.Id;

                // Decrypt sensitive fields
                client.name = _encryptionHelper.Decrypt(client.name).Trim();
                client.surname = _encryptionHelper.Decrypt(client.surname).Trim();
                client.email = _encryptionHelper.Decrypt(client.email).Trim();
                client.phoneNumber = _encryptionHelper.Decrypt(client.phoneNumber).Trim();
                client.address = _encryptionHelper.Decrypt(client.address).Trim();
                client.companyName = _encryptionHelper.Decrypt(client.companyName).Trim();

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
            var clientsRef = GetClientsCollection();
            DocumentReference clientDocRef = clientsRef.Document(clientId);

            DocumentSnapshot snapshot = await clientDocRef.GetSnapshotAsync();
            if (!snapshot.Exists) return null;

            Client client = snapshot.ConvertTo<Client>();
            client.id = snapshot.Id;

            // Decrypt sensitive fields
            client.name = _encryptionHelper.Decrypt(client.name).Trim();
            client.surname = _encryptionHelper.Decrypt(client.surname).Trim();
            client.email = client.email.Trim();
            client.phoneNumber = _encryptionHelper.Decrypt(client.phoneNumber).Trim();
            client.address = _encryptionHelper.Decrypt(client.address).Trim()   ;
            client.companyName = _encryptionHelper.Decrypt(client.companyName).Trim();

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
            var clientsRef = GetClientsCollection();
            DocumentReference clientDocRef = clientsRef.Document(dto.Id);

            DocumentSnapshot snapshot = await clientDocRef.GetSnapshotAsync();
            if (!snapshot.Exists) throw new Exception("Client not found");

            Client client = snapshot.ConvertTo<Client>();

            // Split full name into name + surname if provided
            if (!string.IsNullOrWhiteSpace(dto.FullName))
            {
                var nameParts = dto.FullName.Trim().Split(' ', 2);
                if (nameParts.Length > 0)
                    client.name = _encryptionHelper.Encrypt(nameParts[0]);
                if (nameParts.Length > 1)
                    client.surname = _encryptionHelper.Encrypt(nameParts[1]);
            }

            // Update only provided fields
            if (!string.IsNullOrWhiteSpace(dto.CompanyName))
                client.companyName = _encryptionHelper.Encrypt(dto.CompanyName);

            if (!string.IsNullOrWhiteSpace(dto.Email))
                client.email = _encryptionHelper.Encrypt(dto.Email);

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                client.phoneNumber = _encryptionHelper.Encrypt(dto.PhoneNumber);

            if (!string.IsNullOrWhiteSpace(dto.Address))
                client.address = _encryptionHelper.Encrypt(dto.Address);

            await clientDocRef.SetAsync(client, SetOptions.Overwrite);
        }


        public async Task DeleteClientAsync(string clientId)
        {
            try
            {
                var clientsRef = GetClientsCollection();
                DocumentReference clientDocRef = clientsRef.Document(clientId);

                await clientDocRef.DeleteAsync();
                Console.WriteLine($"Client {clientId} deleted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting client: {ex.Message}");
            }
        }

        private CollectionReference GetClientsCollection()
        {
            return _firestoreDb.Collection("clients");
        }

        public async Task<(bool Success, string ErrorMessage, string TempPassword)> AddEmployeeAsync(Employee employee)
        {
            try
            {
                string managerUid = "daMmNRUlirZSsh4zC1c3N7AtqCG2";

                // Generate the same temp password pattern as Android
                employee.Password = employee.IdNumber.Substring(employee.IdNumber.Length - 6) + "@QC";
                employee.Role = "Employee";

                // Create user in Firebase Authentication
                var userRecordArgs = new UserRecordArgs
                {
                    Email = employee.Email,
                    EmailVerified = false,
                    Password = employee.Password,
                    DisplayName = employee.FullName,
                    Disabled = false
                };

                UserRecord newUser = await FirebaseAuth.DefaultInstance.CreateUserAsync(userRecordArgs);

                // Set UID and encrypt fields
                employee.Uid = newUser.Uid;

                var encryptedEmployee = new Employee
                {
                    Uid = employee.Uid,
                    IdNumber = _encryptionHelper.Encrypt(employee.IdNumber),
                    Name = _encryptionHelper.Encrypt(employee.Name),
                    Surname = _encryptionHelper.Encrypt(employee.Surname),
                    FullName = _encryptionHelper.Encrypt(employee.FullName ?? string.Empty),
                    Email = _encryptionHelper.Encrypt(employee.Email),
                    PhoneNumber = _encryptionHelper.Encrypt(employee.PhoneNumber),
                    Role = _encryptionHelper.Encrypt(employee.Role),
                    CreatedAt = employee.CreatedAtDateTime
                };

                var employeesRef = GetEmployeesCollection(managerUid);
                await employeesRef.Document(encryptedEmployee.Uid).SetAsync(encryptedEmployee);

                return (true, null, employee.Password);
            }
            catch (FirebaseAuthException ex)
            {
                return (false, $"Firebase Auth Error: {ex.Message}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Error adding employee: {ex.Message}", null);
            }
        }

        public async Task<List<Employee>> GetAllEmployeesAsync()
        {
            try
            {
                var employeesRef = GetEmployeesCollection();

                QuerySnapshot snapshot = await employeesRef.GetSnapshotAsync();

                List<Employee> employees = new List<Employee>();

                foreach (var doc in snapshot.Documents)
                {
                    Employee employee = doc.ConvertTo<Employee>();

                    // Decrypt sensitive fields
                    employee.Name = _encryptionHelper.Decrypt(employee.Name);
                    employee.Surname = _encryptionHelper.Decrypt(employee.Surname);
                    employee.FullName = _encryptionHelper.Decrypt(employee.FullName ?? string.Empty);
                    employee.IdNumber = _encryptionHelper.Decrypt(employee.IdNumber);
                    employee.Email = _encryptionHelper.Decrypt(employee.Email);
                    employee.PhoneNumber = _encryptionHelper.Decrypt(employee.PhoneNumber);
                    employee.Role = _encryptionHelper.Decrypt(employee.Role);

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

        public async Task<Employee> GetEmployeeByIdAsync(string employeeId)
        {
            var employeesRef = GetEmployeesCollection();
            DocumentReference employeeDocRef = employeesRef.Document(employeeId);

            DocumentSnapshot snapshot = await employeeDocRef.GetSnapshotAsync();
            if (!snapshot.Exists) return null;

            Employee employee = snapshot.ConvertTo<Employee>();
            employee.Uid = snapshot.Id;

            // Decrypt sensitive fields
            employee.Name = _encryptionHelper.Decrypt(employee.Name).Trim();
            employee.Surname = _encryptionHelper.Decrypt(employee.Surname).Trim();
            employee.Email = _encryptionHelper.Decrypt(employee.Email).Trim();
            employee.PhoneNumber = _encryptionHelper.Decrypt(employee.PhoneNumber).Trim();

            // Handle createdAt safely
            if (employee.CreatedAt is Timestamp ts)
                employee.CreatedAtDateTime = ts.ToDateTime();
            else if (employee.CreatedAt is long unixTimestamp)
                employee.CreatedAtDateTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
            else
                employee.CreatedAtDateTime = DateTime.UtcNow;

            return employee;
        }

        public async Task UpdateEmployeeAsync(EmployeeUpdateDto dto)
        {
            var employeesRef = GetEmployeesCollection();
            DocumentReference employeeDocRef = employeesRef.Document(dto.Id);

            DocumentSnapshot snapshot = await employeeDocRef.GetSnapshotAsync();
            if (!snapshot.Exists) throw new Exception("Employee not found");

            Employee employee = snapshot.ConvertTo<Employee>();

            // Split full name into first + last name if provided
            if (!string.IsNullOrWhiteSpace(dto.FullName))
            {
                var nameParts = dto.FullName.Trim().Split(' ', 2);
                if (nameParts.Length > 0)
                    employee.Name = _encryptionHelper.Encrypt(nameParts[0]);
                if (nameParts.Length > 1)
                    employee.Surname = _encryptionHelper.Encrypt(nameParts[1]);
            }

            // Update other fields if provided
            if (!string.IsNullOrWhiteSpace(dto.Email))
                employee.Email = _encryptionHelper.Encrypt(dto.Email);

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                employee.PhoneNumber = _encryptionHelper.Encrypt(dto.PhoneNumber);

            await employeeDocRef.SetAsync(employee, SetOptions.Overwrite);
        }

        public async Task DeleteEmployeeAsync(string employeeId)
        {
            try
            {
                var employeesRef = GetEmployeesCollection();
                DocumentReference employeeDocRef = employeesRef.Document(employeeId);

                await employeeDocRef.DeleteAsync();
                Console.WriteLine($"Employee {employeeId} deleted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting employee: {ex.Message}");
                throw; // propagate error so controller can return 500
            }
        }

        private CollectionReference GetEmployeesCollection(string userId = "daMmNRUlirZSsh4zC1c3N7AtqCG2")
        {
            return _firestoreDb.Collection("users").Document(userId).Collection("employees");
        }

        public async Task<(bool Success, string ErrorMessage, string QuoteId)> AddQuotationAsync(Quotation quotation)
        {
            try
            {
                if (quotation == null)
                    return (false, "Quotation was null", null);

                

                // Prepare quotation metadata & totals
                await PrepareQuotationMetadataAsync(quotation);
                CalculateQuotationTotals(quotation);

                // Generate PDF
                string outputPdfPath = await GenerateQuotationPdfAsync(quotation);

                if (!File.Exists(outputPdfPath))
                    return (false, $"PDF generation failed at {outputPdfPath}", null);

                // Send quotation email
                await SendQuotationEmailAsync(quotation, outputPdfPath);

                // Encrypt & store in Firestore
                await SaveQuotationToFirestoreAsync(quotation);

                return (true, null, quotation.id);
            }
            catch (Exception ex)
            {
                return (false, $"Error adding quotation: {ex.Message}", null);
            }
        }

        private async Task PrepareQuotationMetadataAsync(Quotation quotation)
        {
            // Get reference to the shared Firestore counter
            var counterRef = _firestoreDb.Collection("settings").Document("quote_counter");

            // Safely increment the counter using a Firestore transaction
            long nextQuoteNumber = await _firestoreDb.RunTransactionAsync(async transaction =>
            {
                var snapshot = await transaction.GetSnapshotAsync(counterRef);

                long lastNumber = snapshot.ContainsField("lastQuoteNumber")
                    ? snapshot.GetValue<long>("lastQuoteNumber")
                    : 10000;

                long nextNumber = lastNumber + 1;

                transaction.Update(counterRef, "lastQuoteNumber", nextNumber);
                return nextNumber;
            });

            // Now create the new quote document with the correct metadata
            var quotesRef = _firestoreDb.Collection("quotes");
            var reservedDocRef = quotesRef.Document();

            quotation.id = reservedDocRef.Id;
            quotation.quoteNumber = $"#{nextQuoteNumber}";
            quotation.createdAt = Timestamp.FromDateTime(DateTime.UtcNow);
            quotation.secureToken = Guid.NewGuid().ToString("N");
        }

        private async Task<long> GetNextQuoteNumberAsync()
        {
            var settingsRef = _firestoreDb.Collection("settings").Document("quote_counter");

            return await _firestoreDb.RunTransactionAsync(async transaction =>
            {
                var snapshot = await transaction.GetSnapshotAsync(settingsRef);
                long lastNumber = snapshot.ContainsField("lastQuoteNumber")
                    ? snapshot.GetValue<long>("lastQuoteNumber")
                    : 10000;

                long nextNumber = lastNumber + 1;
                transaction.Update(settingsRef, "lastQuoteNumber", nextNumber);
                return nextNumber;
            });
        }

        private void CalculateQuotationTotals(Quotation quotation)
        {
            double total = 0.0;
            if (quotation.Items == null) return;

            foreach (var item in quotation.Items)
            {
                if (item == null) continue;
                item.quantity = Math.Max(item.quantity, 0);
                item.unitPrice = Math.Max(item.unitPrice, 0);
                item.amount = item.quantity * item.unitPrice;
                total += item.amount;
            }

            quotation.amount = total;
        }

        private async Task<string> GenerateQuotationPdfAsync(Quotation quotation)
        {
            string safeClientName = string.IsNullOrWhiteSpace(quotation.clientName)
                ? "Client"
                : string.Join("_", quotation.clientName.Split(Path.GetInvalidFileNameChars()));

            quotation.pdfFileName = $"Quotation_{safeClientName}_{quotation.quoteNumber}.pdf";

            string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Quotation", "QuotationTemplate.docx");
            string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedQuotationPdfs");
            Directory.CreateDirectory(outputDir);

            string docxPath = Path.Combine(outputDir, $"{Path.GetFileNameWithoutExtension(quotation.pdfFileName)}.docx");
            string pdfPath = Path.Combine(outputDir, quotation.pdfFileName);

            var wordDoc = new Document();
            wordDoc.LoadFromFile(templatePath);

            ReplaceText(wordDoc, "{{ClientName}}", quotation.clientName);
            ReplaceText(wordDoc, "{{ClientEmail}}", quotation.email);
            ReplaceText(wordDoc, "{{QuoteNumber}}", quotation.quoteNumber);
            ReplaceText(wordDoc, "{{QuoteDate}}", quotation.createdAt.ToDateTime().ToString("yyyy/MM/dd"));

            InsertItemsTable(wordDoc, quotation);

            wordDoc.SaveToFile(docxPath, FileFormat.Docx);
            wordDoc.SaveToFile(pdfPath, FileFormat.PDF);

            return pdfPath;
        }

        private void ReplaceText(Document wordDoc, string placeholder, string value)
        {
            var selection = wordDoc.FindString(placeholder, true, true);
            if (selection != null)
            {
                var range = selection.GetAsOneRange();
                range.Text = value ?? string.Empty;
                range.CharacterFormat.FontName = "Poppins";
                range.CharacterFormat.FontSize = 11;
            }
        }

        private void InsertItemsTable(Document wordDoc, Quotation quotation)
        {
            var section = wordDoc.Sections[0];
            var itemsTable = section.AddTable(true);
            int totalRows = (quotation.Items?.Count ?? 0) + 2;
            itemsTable.ResetCells(totalRows, 4);

            // Header row
            string[] headers = { "QTY", "PRODUCT DESCRIPTION", "UNIT PRICE", "AMOUNT" };
            var headerRow = itemsTable.Rows[0];
            for (int i = 0; i < headers.Length; i++)
            {
                var text = headerRow.Cells[i].AddParagraph().AppendText(headers[i]);
                text.CharacterFormat.Bold = true;
                text.CharacterFormat.FontName = "Poppins";
                text.CharacterFormat.FontSize = 11;
            }

            // Item rows
            for (int i = 0; i < (quotation.Items?.Count ?? 0); i++)
            {
                var item = quotation.Items[i];
                var row = itemsTable.Rows[i + 1];

                row.Cells[0].AddParagraph().AppendText(item.quantity.ToString());
                row.Cells[1].AddParagraph().AppendText(item.description ?? "");
                row.Cells[2].AddParagraph().AppendText($"R{item.unitPrice:0.00}");
                row.Cells[3].AddParagraph().AppendText($"R{item.amount:0.00}");
            }

            // Total row
            var totalRow = itemsTable.Rows[totalRows - 1];
            totalRow.Cells[2].AddParagraph().AppendText("Total Amount:").CharacterFormat.Bold = true;
            totalRow.Cells[3].AddParagraph().AppendText($"R{quotation.amount:0.00}").CharacterFormat.Bold = true;

            // Replace placeholder
            var placeholder = wordDoc.FindString("{{ItemsTable}}", true, true);
            if (placeholder != null)
            {
                var para = placeholder.GetAsOneRange().OwnerParagraph;
                var body = para.OwnerTextBody;
                int idx = body.ChildObjects.IndexOf(para);
                body.ChildObjects.Remove(para);
                body.ChildObjects.Insert(idx, itemsTable);
            }
        }

        private async Task SendQuotationEmailAsync(Quotation quotation, string pdfPath)
        {
            string userId = "daMmNRUlirZSsh4zC1c3N7AtqCG2";

            string baseUrl = "https://us-central1-insy7315-database2.cloudfunctions.net/updateQuoteStatus";

            string acceptUrl = $"{baseUrl}?userId={userId}&quoteId={quotation.id}&status=Accepted&token={quotation.secureToken}";
            string declineUrl = $"{baseUrl}?userId={userId}&quoteId={quotation.id}&status=Declined&token={quotation.secureToken}";

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("Quality Copiers", _senderEmail));
            email.To.Add(new MailboxAddress(quotation.clientName, quotation.email));
            email.Subject = $"Quotation {quotation.quoteNumber}";

            var builder = new BodyBuilder();
            var htmlBody = $@"
                <p>Dear {System.Net.WebUtility.HtmlEncode(quotation.clientName)},</p>
                <p>Please review your quotation.</p>
                <p>
                    Please follow this link to 
                    <a href='{acceptUrl}'>Accept the Quotation</a>.
                </p>
                <p>
                    Alternatively, you can 
                    <a href='{declineUrl}'>Decline this Quotation</a>.
                </p>

                <p>The PDF is attached for your reference.</p>
                <p>Kind Regards,<br/>Quality Copiers</p>";

            builder.HtmlBody = htmlBody;
            builder.Attachments.Add(pdfPath);

            email.Body = builder.ToMessageBody();

            await _mailService.SendEmailAsync(email);
        }

        private async Task SaveQuotationToFirestoreAsync(Quotation quotation)
        {
            var quotesRef = _firestoreDb.Collection("quotes");
            var docRef = quotesRef.Document(quotation.id);

            var encrypted = new Quotation
            {
                id = quotation.id ?? "",
                clientName = _encryptionHelper.Encrypt(quotation.clientName ?? ""),
                companyName = _encryptionHelper.Encrypt(quotation.companyName ?? ""),
                email = _encryptionHelper.Encrypt(quotation.email ?? ""),
                phone = _encryptionHelper.Encrypt(quotation.phone ?? ""),
                quoteNumber = _encryptionHelper.Encrypt(quotation.quoteNumber ?? ""),
                createdAt = quotation.createdAt,
                amount = quotation.amount,
                secureToken = quotation.secureToken ?? "",
                pdfFileName = _encryptionHelper.Encrypt(quotation.pdfFileName ?? ""),
                Items = quotation.Items?.Select(i => new QuotationItem
                {
                    description = _encryptionHelper.Encrypt(i.description ?? ""),
                    quantity = i.quantity,
                    unitPrice = i.unitPrice,
                    amount = i.amount
                }).ToList() ?? new List<QuotationItem>()
            };

            await docRef.SetAsync(encrypted);
            await docRef.UpdateAsync("id", quotation.id);
        }


        public async Task<List<Quotation>> GetQuotationsAsync()
        {
            try
            {
                var quotesRef = _firestoreDb.Collection("quotes");
                var snapshot = await quotesRef.GetSnapshotAsync();

                List<Quotation> quotations = new();

                foreach (var doc in snapshot.Documents)
                {
                    if (!doc.Exists) continue;

                    var quotation = doc.ConvertTo<Quotation>();

                    // Decrypt top-level sensitive fields
                    quotation.quoteNumber = _encryptionHelper.Decrypt(quotation.quoteNumber ?? string.Empty);
                    quotation.clientName = _encryptionHelper.Decrypt(quotation.clientName);
                    quotation.companyName = _encryptionHelper.Decrypt(quotation.companyName);
                    quotation.email = _encryptionHelper.Decrypt(quotation.email);
                    quotation.phone = _encryptionHelper.Decrypt(quotation.phone);

                    // Decrypt item descriptions
                    if (quotation.Items != null)
                    {
                        foreach (var item in quotation.Items)
                        {
                            if (item == null) continue;
                            item.description = _encryptionHelper.Decrypt(item.description);
                        }
                    }

                    quotations.Add(quotation);
                }

                return quotations;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching quotations: {ex.Message}");
                return new List<Quotation>();
            }
        }

        public async Task<(byte[] PdfBytes, string PdfFileName)> GenerateQuotationPdfBytesAsync(Quotation quotation)
        {
            if (quotation == null || quotation.Items == null || !quotation.Items.Any())
                throw new ArgumentException("Quotation is invalid");

            // Generate the PDF and get the path
            string pdfPath = await GenerateQuotationPdfAsync(quotation);

            if (!File.Exists(pdfPath))
                throw new FileNotFoundException("Failed to generate quotation PDF", pdfPath);

            // Read the PDF bytes
            byte[] pdfBytes = await File.ReadAllBytesAsync(pdfPath);

            // Optionally, clean up DOCX if you want
            string docxPath = Path.Combine(Path.GetDirectoryName(pdfPath), Path.GetFileNameWithoutExtension(pdfPath) + ".docx");
            if (File.Exists(docxPath))
                File.Delete(docxPath);

            // Return bytes and filename
            return (pdfBytes, quotation.pdfFileName);
        }

        public async Task DeleteQuotationAsync(string quoteId)
        {
            try
            {
                DocumentReference quoteDoc = GetQuotationsCollection().Document(quoteId);

                await quoteDoc.DeleteAsync();
                Console.WriteLine($"Quotation {quoteId} deleted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting quotation: {ex.Message}");
            }
        }

        private CollectionReference GetQuotationsCollection()
        {
            return _firestoreDb.Collection("quotes");
        }

        public async Task<List<Invoice>> GetInvoicesAsync()
        {
            var invoicesRef = GetInvoicesCollection();
            var snapshot = await invoicesRef.GetSnapshotAsync();

            var invoices = new List<Invoice>();

            foreach (var doc in snapshot.Documents)
            {
                var invoice = doc.ConvertTo<Invoice>();

                // Decrypt sensitive fields
                invoice.ClientName = _encryptionHelper.Decrypt(invoice.ClientName);
                invoice.CompanyName = _encryptionHelper.Decrypt(invoice.CompanyName);
                invoice.Email = _encryptionHelper.Decrypt(invoice.Email);
                invoice.Phone = _encryptionHelper.Decrypt(invoice.Phone);
                invoice.QuoteNumber = _encryptionHelper.Decrypt(invoice.QuoteNumber ?? string.Empty);
                invoice.Status = invoice.Status;

                if (invoice.Items != null)
                {
                    foreach (var item in invoice.Items)
                    {
                        item.Description = _encryptionHelper.Decrypt(item.Description);
                    }
                }

                invoices.Add(invoice);
            }

            return invoices;
        }

        //Get invoice details by ID for payments
        public async Task<Invoice?> GetInvoiceDetailsAsync(string id)
        {
            try
            {
                var invoiceRef = GetInvoicesCollection().Document(id);
                var snapshot = await invoiceRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                    return null;

                var invoice = snapshot.ConvertTo<Invoice>();

                // Decrypt sensitive fields
                invoice.ClientName = _encryptionHelper.Decrypt(invoice.ClientName);
                invoice.CompanyName = _encryptionHelper.Decrypt(invoice.CompanyName);
                invoice.Email = _encryptionHelper.Decrypt(invoice.Email ?? string.Empty);
                invoice.Phone = _encryptionHelper.Decrypt(invoice.Phone ?? string.Empty);
                invoice.QuoteNumber = _encryptionHelper.Decrypt(invoice.QuoteNumber ?? string.Empty);

                if (invoice.Items != null)
                {
                    foreach (var item in invoice.Items)
                    {
                        item.Description = _encryptionHelper.Decrypt(item.Description ?? string.Empty);
                    }
                }

                return invoice;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving invoice: {ex.Message}");
                throw;
            }
        }

        // Generating invoice PDF bytes - used for downloading/saving invoice

        public async Task<(byte[] PdfBytes, string PdfFileName)> GenerateInvoicePdfBytesAsync(Invoice invoice)
        {
            // Validate the invoice
            if (invoice == null || invoice.Items == null || !invoice.Items.Any())
                throw new ArgumentException("Invoice is invalid");

            // Define template path
            string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Invoice", "InvoiceTemplate.docx");
            if (!File.Exists(templatePath))
                throw new FileNotFoundException("Invoice template not found.", templatePath);

            // Define output directory
            string generatedDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedInvoicePdfs");
            Directory.CreateDirectory(generatedDir);

            // Create safe client name and unique filename
            string safeClientName = string.IsNullOrWhiteSpace(invoice.ClientName)
                ? "Client"
                : string.Join("_", invoice.ClientName.Split(Path.GetInvalidFileNameChars()));

            string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            string pdfFileName = $"Invoice_{safeClientName}_{invoice.InvoiceNumber}_{timestamp}.pdf";

            string tempDocxPath = Path.Combine(generatedDir, $"{Path.GetFileNameWithoutExtension(pdfFileName)}.docx");
            string outputPdfPath = Path.Combine(generatedDir, pdfFileName);

            // Generate the PDF
            GenerateInvoicePdf(invoice, templatePath, tempDocxPath, outputPdfPath);

            if (!File.Exists(outputPdfPath))
                throw new FileNotFoundException("Failed to generate invoice PDF", outputPdfPath);

            // Read the bytes
            var pdfBytes = await File.ReadAllBytesAsync(outputPdfPath);

            // Clean up temporary files
            if (File.Exists(tempDocxPath))
                File.Delete(tempDocxPath);

            // Return both the PDF bytes and filename
            return (pdfBytes, pdfFileName);
        }

        public async Task<bool> GenerateAndSendInvoiceAsync(Invoice invoice)
        {
            try
            {
                // validating invoice
                if (invoice == null)
                    throw new ArgumentNullException(nameof(invoice), "Invoice cannot be null.");
                if (invoice.Items == null || !invoice.Items.Any())
                    throw new ArgumentException("Invoice items are missing.", nameof(invoice));

                // defining the file paths ---
                string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Invoice", "InvoiceTemplate.docx");
                if (!File.Exists(templatePath))
                    throw new FileNotFoundException("Invoice template not found.", templatePath);

                // defining output directory and file names
                string generatedDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedInvoicePdfs");
                Directory.CreateDirectory(generatedDir);

                // sanitizing client name for file system
                string safeClientName = string.IsNullOrWhiteSpace(invoice.ClientName)
                    ? "Client"
                    : string.Join("_", invoice.ClientName.Split(Path.GetInvalidFileNameChars()));

                // unique timestamp for file name
                string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                string pdfFileName = $"Invoice {safeClientName} {invoice.InvoiceNumber} {timestamp}.pdf";

                // temporary DOCX and final PDF paths
                string tempDocxPath = Path.Combine(generatedDir, $"{Path.GetFileNameWithoutExtension(pdfFileName)}.docx");
                string outputPdfPath = Path.Combine(generatedDir, pdfFileName);

                // generating the pdf 
                GenerateInvoicePdf(invoice, templatePath, tempDocxPath, outputPdfPath);

                // validating the pdf file content before sending
                if (!File.Exists(outputPdfPath))
                    throw new FileNotFoundException("Failed to generate invoice PDF.", outputPdfPath);

                // basic file checks
                var fi = new FileInfo(outputPdfPath);
                Console.WriteLine($"[DEBUG] Generated PDF size: {fi.Length} bytes at {outputPdfPath}");
                if (fi.Length < 100)
                    throw new InvalidOperationException($"Generated PDF seems too small ({fi.Length} bytes).");

                // quick header check
                using (var fs = File.OpenRead(outputPdfPath))
                {
                    var header = new byte[5];
                    fs.Read(header, 0, header.Length);
                    var headerStr = System.Text.Encoding.ASCII.GetString(header);
                    Console.WriteLine($"[DEBUG] PDF header: {headerStr}");
                    if (!headerStr.StartsWith("%PDF"))
                        throw new InvalidOperationException("Generated file is not a valid PDF (missing %PDF header).");
                }

                // sending the email
                await SendInvoiceEmailAsync(invoice, outputPdfPath);

                // cleaning up the temp docx file
                if (File.Exists(tempDocxPath))
                    File.Delete(tempDocxPath);

                Console.WriteLine($"Invoice {invoice.InvoiceNumber} successfully generated and sent.");
                return true;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Validation failed: {ex.Message}");
                return false;
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"File error: {ex.Message} (File: {ex.FileName})");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to generate/send invoice {invoice?.InvoiceNumber ?? "N/A"}: {ex}");
                return false;
            }
        }

        private void GenerateInvoicePdf(Invoice invoice, string templatePath, string tempDocxPath, string outputPdfPath)
        {
            if (invoice == null) throw new ArgumentNullException(nameof(invoice));
            if (invoice.Items == null || !invoice.Items.Any())
                throw new ArgumentException("Invoice must have at least one item.", nameof(invoice));

            // load the word template
            Document wordDoc = new Document();
            wordDoc.LoadFromFile(templatePath);

            // Helper to replace placeholders
            void ReplaceText(string placeholder, string value)
            {
                TextSelection selection = wordDoc.FindString(placeholder, true, true);
                if (selection != null)
                {
                    TextRange range = selection.GetAsOneRange();
                    range.Text = value ?? string.Empty;
                    range.CharacterFormat.FontName = "Poppins";
                    range.CharacterFormat.FontSize = 11;
                }
                else
                {
                    Console.WriteLine($"[DEBUG] Placeholder '{placeholder}' not found in template.");
                }
            }

            ReplaceText("{{ClientName}}", invoice.ClientName);
            ReplaceText("{{ClientEmail}}", invoice.Email);
            ReplaceText("{{InvoiceNumber}}", invoice.InvoiceNumber);
            DateTime invoiceDate = invoice.CreatedAt.HasValue ? invoice.CreatedAt.Value.ToDateTime() : DateTime.UtcNow;
            ReplaceText("{{InvoiceDate}}", invoiceDate.ToString("yyyy/MM/dd"));

            // Build Items Table
            Section section = wordDoc.Sections[0];
            Table itemsTable = section.AddTable(true);

            int itemCount = invoice.Items.Count;
            int totalRows = itemCount + 2; // header + items + total
            itemsTable.ResetCells(totalRows, 4);

            // Table header
            string[] headers = { "QTY", "PRODUCT DESCRIPTION", "UNIT PRICE", "AMOUNT" };
            TableRow headerRow = itemsTable.Rows[0];
            for (int i = 0; i < headers.Length; i++)
            {
                TextRange headerText = headerRow.Cells[i].AddParagraph().AppendText(headers[i]);
                headerText.CharacterFormat.Bold = true;
                headerText.CharacterFormat.FontName = "Poppins";
                headerText.CharacterFormat.FontSize = 11;
                headerRow.Cells[i].CellFormat.VerticalAlignment = VerticalAlignment.Middle;
                headerRow.Cells[i].Width = i == 1 ? 220f : 80f;
            }

            // Item rows
            double totalAmount = 0;
            for (int i = 0; i < itemCount; i++)
            {
                var item = invoice.Items[i];
                double qty = Math.Max(0, item.Quantity);
                double price = Math.Max(0, item.UnitPrice);
                double rowTotal = qty * price;
                totalAmount += rowTotal;

                TableRow row = itemsTable.Rows[i + 1];
                row.Cells[0].AddParagraph().AppendText(qty.ToString()).CharacterFormat.FontName = "Poppins";
                row.Cells[1].AddParagraph().AppendText(item.Description ?? string.Empty).CharacterFormat.FontName = "Poppins";
                row.Cells[2].AddParagraph().AppendText($"R{price:0.00}").CharacterFormat.FontName = "Poppins";
                row.Cells[3].AddParagraph().AppendText($"R{rowTotal:0.00}").CharacterFormat.FontName = "Poppins";
            }

            // Total row
            TableRow totalRow = itemsTable.Rows[totalRows - 1];
            totalRow.Cells[2].AddParagraph().AppendText("Total Amount:").CharacterFormat.Bold = true;
            totalRow.Cells[2].Paragraphs[0].Format.HorizontalAlignment = HorizontalAlignment.Right;
            totalRow.Cells[3].AddParagraph().AppendText($"R{totalAmount:0.00}").CharacterFormat.Bold = true;
            totalRow.Cells[3].Paragraphs[0].Format.HorizontalAlignment = HorizontalAlignment.Right;

            // Insert table at placeholder
            TextSelection placeholder = wordDoc.FindString("{{ItemsTable}}", true, true);
            if (placeholder != null)
            {
                Paragraph para = placeholder.GetAsOneRange().OwnerParagraph;
                Body body = para.OwnerTextBody;
                int index = body.ChildObjects.IndexOf(para);
                body.ChildObjects.Remove(para);
                body.ChildObjects.Insert(index, itemsTable);
                Console.WriteLine("[DEBUG] Inserted items table at placeholder.");
            }
            else
            {
                // fallback: append to section body (log so you can inspect template)
                section.Body.ChildObjects.Add(itemsTable);
                Console.WriteLine("[DEBUG] Placeholder '{{ItemsTable}}' not found — appended table to section body.");
            }

            // Save files
            wordDoc.SaveToFile(tempDocxPath, FileFormat.Docx);
            wordDoc.SaveToFile(outputPdfPath, FileFormat.PDF);

            Console.WriteLine($"[DEBUG] Saved DOCX: {tempDocxPath}");
            Console.WriteLine($"[DEBUG] Saved PDF: {outputPdfPath}");
        }

        private async Task SendInvoiceEmailAsync(Invoice invoice, string pdfPath)
        {
            if (invoice == null || string.IsNullOrWhiteSpace(invoice.Email) || !File.Exists(pdfPath))
                throw new ArgumentException("Invalid invoice or PDF path");

            // Build the email
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("Quality Copiers", _senderEmail));
            emailMessage.To.Add(new MailboxAddress(invoice.ClientName, invoice.Email));
            emailMessage.Subject = $"Invoice {invoice.InvoiceNumber}";

            var builder = new BodyBuilder();
            builder.HtmlBody = $@"
                 <p>Dear {System.Net.WebUtility.HtmlEncode(invoice.ClientName)},</p>
                 <p>Thank you for your business. Please find your invoice attached.</p>
                 <p>The status of this invoice is <strong>{System.Net.WebUtility.HtmlEncode(invoice.Status)}</strong>.</p>
                 <p>Kind regards,<br/>Quality Copiers</p>";

            // Attach PDF using a FileStream kept open for the send operation
            using (var pdfStream = File.OpenRead(pdfPath))
            {
                // Use MimePart with MimeContent constructed from the stream (like your quotations code)
                var pdfAttachment = new MimePart("application", "pdf")
                {
                    Content = new MimeContent(pdfStream, ContentEncoding.Default), // let MimeKit set encoding on transfer
                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    FileName = Path.GetFileName(pdfPath)
                };

                var multipart = new Multipart("mixed");
                multipart.Add(new TextPart("html") { Text = builder.HtmlBody });
                multipart.Add(pdfAttachment);

                emailMessage.Body = multipart;

                /*// Now send (stream must remain open until SendAsync completes)
                using var smtp = new MailKit.Net.Smtp.SmtpClient();
                await smtp.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync("zimkhitha.sasanti@gmail.com", "pflq gfdg xyeb pitx");
                await smtp.SendAsync(emailMessage);
                await smtp.DisconnectAsync(true);*/

                await _mailService.SendEmailAsync(emailMessage);
            }
        }

        public async Task UpdateInvoiceStatusAsync(string invoiceId, string status)
        {
            // Access the invoice document inside the user's "invoices" subcollection
            var docRef = GetInvoicesCollection().Document(invoiceId);

            var updates = new Dictionary<string, object>
            {
                { "status", status }
            };

            await docRef.UpdateAsync(updates);
        }

        public async Task DeleteInvoiceAsync(string invoiceId)
        {
            try
            {
                DocumentReference invoiceDoc = GetInvoicesCollection().Document(invoiceId);

                await invoiceDoc.DeleteAsync();
                Console.WriteLine($"Invoice {invoiceId} deleted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting quotation: {ex.Message}");
            }
        }

        private CollectionReference GetInvoicesCollection()
        {
            return _firestoreDb.Collection("invoices");
        }

          public async Task<Dictionary<string, object>> GetManagerDataAsync(string userId)
        {
            var docRef = _firestoreDb
                .Collection("users")
                .Document(userId)
                .Collection("manager_data")
                .Document(userId);

            var snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
                return snapshot.ToDictionary();
            else
                throw new Exception("User data not found in Firestore.");
        }

        public async Task<(bool Success, string Message)> UpdateManagerDataAsync(string userId, Dictionary<string, object> updatedData)
        {
            if (string.IsNullOrEmpty(userId))
                return (false, "User ID cannot be null or empty.");

            if (updatedData == null || updatedData.Count == 0)
                return (false, "No update data provided.");

            try
            {
                DocumentReference docRef = _firestoreDb.Collection("users").Document(userId).Collection("manager_data").Document(userId);

                // Add a timestamp to track last update
                updatedData["lastUpdated"] = Timestamp.GetCurrentTimestamp();

                // ✅ Merge ensures we only update provided fields
                await docRef.SetAsync(updatedData, SetOptions.MergeAll);

                return (true, "Profile updated successfully.");
            }
            catch (Grpc.Core.RpcException grpcEx)
            {
                Console.WriteLine($"🔥 Firestore RPC error for user {userId}: {grpcEx.Status.Detail}");
                return (false, $"Firestore RPC error: {grpcEx.Status.Detail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔥 Firestore update failed for user {userId}: {ex.Message}");
                return (false, $"Unexpected error updating Firestore: {ex.Message}");
            }
        }

        public async Task<Dictionary<string, object>> GetUserDetailsAsync(string userId)
        {
            string userDocument = "daMmNRUlirZSsh4zC1c3N7AtqCG2";

            var docRef = _firestoreDb
                .Collection("users")
                .Document(userDocument)
                .Collection("employees") // Adjust collection name if needed
                .Document(userId);

            var snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists)
                throw new Exception("User data not found in Firestore.");

            var encryptedData = snapshot.ToDictionary();
            var decryptedData = new Dictionary<string, object>();

            foreach (var kvp in encryptedData)
            {
                try
                {
                    // Only decrypt string values
                    if (kvp.Value is string encryptedValue)
                    {
                        decryptedData[kvp.Key] = _encryptionHelper.Decrypt(encryptedValue);
                    }
                    else
                    {
                        // Keep non-string fields (like DateTime, bool, numbers) as is
                        decryptedData[kvp.Key] = kvp.Value;
                    }
                }
                catch
                {
                    // If decryption fails (e.g., field wasn't encrypted), store the original value
                    decryptedData[kvp.Key] = kvp.Value;
                }
            }

            return decryptedData;
        }

        public async Task<(bool Success, string Message)> UpdateUserDetailsAsync(string userId, Dictionary<string, object> updatedData)
        {
            if (string.IsNullOrEmpty(userId))
                return (false, "User ID cannot be null or empty.");

            if (updatedData == null || updatedData.Count == 0)
                return (false, "No update data provided.");

            try
            {
                string userDocument = "daMmNRUlirZSsh4zC1c3N7AtqCG2";

                DocumentReference docRef = _firestoreDb
                    .Collection("users")
                    .Document(userDocument)
                    .Collection("employees") // verify collection name when they're done
                    .Document(userId);

                // 🔒 Encrypt all data before saving
                var encryptedData = new Dictionary<string, object>();
                foreach (var entry in updatedData)
                {
                    // Don’t encrypt metadata like timestamps
                    if (entry.Key.Equals("lastUpdated", StringComparison.OrdinalIgnoreCase))
                    {
                        encryptedData[entry.Key] = entry.Value;
                    }
                    else
                    {
                        // Encrypt only string values; preserve non-string (like bool or numbers)
                        if (entry.Value is string strValue)
                            encryptedData[entry.Key] = _encryptionHelper.Encrypt(strValue);
                        else
                            encryptedData[entry.Key] = entry.Value;
                    }
                }

                // 🕒 Add or overwrite the lastUpdated field
                encryptedData["lastUpdated"] = Timestamp.GetCurrentTimestamp();

                // ✅ Merge ensures only provided fields are updated
                await docRef.SetAsync(encryptedData, SetOptions.MergeAll);

                return (true, "Profile updated successfully.");
            }
            catch (Grpc.Core.RpcException grpcEx)
            {
                Console.WriteLine($"Firestore RPC error for user {userId}: {grpcEx.Status.Detail}");
                return (false, $"Firestore RPC error: {grpcEx.Status.Detail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Firestore update failed for user {userId}: {ex.Message}");
                return (false, $"Unexpected error updating Firestore: {ex.Message}");
            }
        }

        public async Task<List<Invoice>> GetPaidInvoicesByDateRangeAsync(int months)
        {
            try
            {
                var invoicesRef = _firestoreDb.Collection("invoices");
                var snapshot = await invoicesRef.GetSnapshotAsync();

                DateTime cutoffDate = DateTime.UtcNow.AddMonths(-months);
                List<Invoice> paidInvoices = new();

                // Local safe decryption helper
                string SafeDecrypt(string value)
                {
                    if (string.IsNullOrWhiteSpace(value))
                        return value;

                    try
                    {
                        return _encryptionHelper.Decrypt(value);
                    }
                    catch
                    {
                        // Return the original value if not encrypted or invalid Base64
                        return value;
                    }
                }

                foreach (var doc in snapshot.Documents)
                {
                    if (!doc.Exists) continue;
                    var invoice = doc.ConvertTo<Invoice>();

                    // Use SafeDecrypt for every possibly-encrypted field
                    invoice.ClientName = SafeDecrypt(invoice.ClientName);
                    invoice.CompanyName = SafeDecrypt(invoice.CompanyName);
                    invoice.InvoiceNumber = SafeDecrypt(invoice.InvoiceNumber);
                    invoice.Email = SafeDecrypt(invoice.Email);
                    invoice.Phone = SafeDecrypt(invoice.Phone);

                    if (invoice.CreatedAt == null) continue;
                    DateTime createdAt = invoice.CreatedAt.Value.ToDateTime();

                    if (invoice.Status == "Paid" && createdAt >= cutoffDate)
                    {
                        paidInvoices.Add(invoice);
                    }
                }

                return paidInvoices.OrderByDescending(i => i.CreatedAt).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching paid invoices: {ex.Message}");
                return new List<Invoice>();
            }
        }
        public async Task<(byte[] PdfBytes, string FileName)> GeneratePaymentsReportPdfAsync(List<Invoice> invoices, int months)
        {
            if (invoices == null || !invoices.Any())
                throw new ArgumentException("No invoices found for report generation.");

            string tempDocPath = Path.Combine(Path.GetTempPath(), $"Payments_Report_{months}Months_{DateTime.Now:yyyyMMddHHmmss}.docx");
            string tempPdfPath = Path.ChangeExtension(tempDocPath, ".pdf");

            Document document = new Document();
            Section section = document.AddSection();

            // Title
            Paragraph title = section.AddParagraph();
            TextRange titleText = title.AppendText($"Payments Received Report (Last {months} Months)");
            titleText.CharacterFormat.FontName = "Poppins";
            titleText.CharacterFormat.FontSize = 18;
            titleText.CharacterFormat.Bold = true;
            title.Format.HorizontalAlignment = HorizontalAlignment.Center;
            title.Format.AfterSpacing = 15;

            // Table
            Table table = section.AddTable(true);
            table.ResetCells(invoices.Count + 1, 4); // +1 for header

            // Header row
            string[] headers = { "Client Name", "Company Name", "Invoice Number", "Total Amount" };
            TableRow headerRow = table.Rows[0];
            headerRow.RowFormat.BackColor = Color.LightGray;

            for (int i = 0; i < headers.Length; i++)
            {
                Paragraph p = headerRow.Cells[i].AddParagraph();
                TextRange tr = p.AppendText(headers[i]);
                tr.CharacterFormat.FontName = "Poppins";
                tr.CharacterFormat.FontSize = 12;
                tr.CharacterFormat.Bold = true;
                p.Format.HorizontalAlignment = HorizontalAlignment.Center;
            }

            // Data rows
            double totalAmount = 0;
            for (int i = 0; i < invoices.Count; i++)
            {
                Invoice inv = invoices[i];
                TableRow row = table.Rows[i + 1];

                row.Cells[0].AddParagraph().AppendText(inv.ClientName ?? "-");
                row.Cells[1].AddParagraph().AppendText(inv.CompanyName ?? "-");
                row.Cells[2].AddParagraph().AppendText(inv.InvoiceNumber ?? "-");
                row.Cells[3].AddParagraph().AppendText($"R {inv.TotalAmount:F2}");

                totalAmount += inv.TotalAmount;
            }

            table.TableFormat.Borders.LineWidth = 0.5f;
            table.TableFormat.Borders.Color = Color.LightGray;
            table.Rows[0].Height = 20;
            table.AutoFit(AutoFitBehaviorType.AutoFitToWindow);

            // Total line
            Paragraph totalParagraph = section.AddParagraph();
            totalParagraph.AppendText($"\nTotal Paid Amount: R {totalAmount:F2}");
            totalParagraph.Format.HorizontalAlignment = HorizontalAlignment.Right;
            totalParagraph.Format.AfterSpacing = 10;
            totalParagraph.BreakCharacterFormat.Bold = true;
            totalParagraph.BreakCharacterFormat.FontSize = 12;

            // Footer
            Paragraph footer = section.AddParagraph();
            footer.AppendText($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}");
            footer.Format.HorizontalAlignment = HorizontalAlignment.Right;
            footer.BreakCharacterFormat.FontSize = 10;
            footer.BreakCharacterFormat.Italic = true;
            footer.BreakCharacterFormat.TextColor = Color.Gray;

            // Save DOCX, then export to PDF
            document.SaveToFile(tempDocPath, FileFormat.Docx);
            document.SaveToFile(tempPdfPath, FileFormat.PDF);

            byte[] pdfBytes = await File.ReadAllBytesAsync(tempPdfPath);

            // Cleanup temp files
            if (File.Exists(tempDocPath)) File.Delete(tempDocPath);
            if (File.Exists(tempPdfPath)) File.Delete(tempPdfPath);

            string fileName = $"Payments_Report_{months}Months.pdf";
            return (pdfBytes, fileName);
        }



    }
}