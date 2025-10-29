using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using INSY7315_ElevateDigitalStudios_POE.Models;
using INSY7315_ElevateDigitalStudios_POE.Models.Dtos;
using INSY7315_ElevateDigitalStudios_POE.Security;
using Spire.Doc;
using Spire.Doc.Documents;
using Spire.Doc.Fields;
using MimeKit;

namespace INSY7315_ElevateDigitalStudios_POE.Services
{
    public class FirebaseService
    {
        // setting up firestore connection and encryption helper
        private readonly FirestoreDb _firestoreDb;
        private readonly EncryptionHelper _encryptionHelper;

        // firebase service constructor - initialize firestore connection and encryption helper
        public FirebaseService(EncryptionHelper encryptionHelper)
        {
            // check if firebase app is already initialized
            if (FirebaseApp.DefaultInstance == null)
            {
                // initialize firebase app with default credentials
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.GetApplicationDefault()
                });
            }

            // initialize firestore database and encryption helper
            _firestoreDb = FirestoreDb.Create("insy7315-database");
            _encryptionHelper = encryptionHelper;
        }

        // method to get firestore database instance
        public FirestoreDb GetFirestore()
        {
            return _firestoreDb;
        }

        // method to add a new client to firestore
        public async Task AddClientAsync(Client client)
        {
            // encrypting the sensitive fields
            client.email = _encryptionHelper.Encrypt(client.email);
            client.phoneNumber = _encryptionHelper.Encrypt(client.phoneNumber);
            client.address = _encryptionHelper.Encrypt(client.address);

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
            var clientsRef = GetClientsCollection();
            DocumentReference clientDocRef = clientsRef.Document(clientId);

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
            var clientsRef = GetClientsCollection();
            DocumentReference clientDocRef = clientsRef.Document(dto.Id);

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

        private CollectionReference GetClientsCollection(string userId = "ypqdjnU59xfE6cdE4NoKPAoWPfA2")
        {
            // Navigate to the user document
            DocumentReference userDocRef = _firestoreDb.Collection("users").Document(userId);

            // Return the clients subcollection
            return userDocRef.Collection("clients");
        }

        public async Task<(bool Success, string ErrorMessage)> AddEmployeeAsync(Employee employee)
        {
            try
            {
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

                // Set UID and encrypt sensitive fields before storing
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

                var employeesRef = GetEmployeesCollection();
                await employeesRef.Document(encryptedEmployee.Uid).SetAsync(encryptedEmployee);

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
                var employeesRef = GetEmployeesCollection();

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

        private CollectionReference GetEmployeesCollection(string userId = "ypqdjnU59xfE6cdE4NoKPAoWPfA2")
        {
            return _firestoreDb.Collection("users").Document(userId).Collection("employees");
        }

        public async Task<(bool Success, string ErrorMessage, string QuoteId)> AddQuotationAsync(Quotation quotation)
        {
            try
            {
                if (quotation == null)
                    return (false, "Quotation was null", null);

                // Firestore setup
                string userId = "vz4maSc0vOgouOGPhtdkFzBlceK2";
                DocumentReference userDocRef = _firestoreDb.Collection("users").Document(userId);
                CollectionReference quotesRef = userDocRef.Collection("quotes");

                // Reserve Firestore document ID early
                DocumentReference reservedDocRef = quotesRef.Document();
                quotation.id = reservedDocRef.Id;

                // Generate metadata
                quotation.quoteNumber = $"#{DateTime.UtcNow.Ticks % 1000000:D6}";
                quotation.createdAt = Timestamp.FromDateTime(DateTime.UtcNow);
                quotation.secureToken = Guid.NewGuid().ToString("N");

                // Calculate total amount
                double totalAmount = 0.0;
                if (quotation.Items != null && quotation.Items.Any())
                {
                    foreach (var item in quotation.Items)
                    {
                        if (item == null) continue;
                        item.quantity = Math.Max(item.quantity, 0);
                        item.unitPrice = Math.Max(item.unitPrice, 0.0);
                        item.amount = item.quantity * item.unitPrice;
                        totalAmount += item.amount;
                    }
                }
                quotation.amount = totalAmount;

                // File naming
                string safeClientName = string.IsNullOrWhiteSpace(quotation.clientName)
                    ? "Client"
                    : string.Join("_", quotation.clientName.Split(Path.GetInvalidFileNameChars()));
                quotation.pdfFileName = $"Quotation {safeClientName} {quotation.quoteNumber}.pdf";

                // Template paths
                string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Quotation", "QuotationTemplate.docx");
                string generatedDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedQuotationPdfs");
                Directory.CreateDirectory(generatedDir);

                string tempDocxPath = Path.Combine(generatedDir, $"{Path.GetFileNameWithoutExtension(quotation.pdfFileName)}.docx");
                string outputPdfPath = Path.Combine(generatedDir, quotation.pdfFileName);

                // Load and populate Word template
                Document wordDoc = new Document();
                wordDoc.LoadFromFile(templatePath);

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
                }

                ReplaceText("{{ClientName}}", quotation.clientName);
                ReplaceText("{{ClientEmail}}", quotation.email);
                ReplaceText("{{QuoteNumber}}", quotation.quoteNumber);
                ReplaceText("{{QuoteDate}}", quotation.createdAt.ToDateTime().ToString("yyyy/MM/dd"));

                // Build Items Table
                Section section = wordDoc.Sections[0];
                Table itemsTable = section.AddTable(true);

                int totalRows = (quotation.Items?.Count ?? 0) + 2;
                itemsTable.ResetCells(totalRows, 4);

                // Header row
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
                for (int i = 0; i < (quotation.Items?.Count ?? 0); i++)
                {
                    var item = quotation.Items[i];
                    TableRow row = itemsTable.Rows[i + 1];
                    double rowTotal = item.quantity * item.unitPrice;

                    row.Cells[0].AddParagraph().AppendText(item.quantity.ToString()).CharacterFormat.FontName = "Poppins";
                    row.Cells[1].AddParagraph().AppendText(item.description ?? string.Empty).CharacterFormat.FontName = "Poppins";
                    row.Cells[2].AddParagraph().AppendText($"R{item.unitPrice:0.00}").CharacterFormat.FontName = "Poppins";
                    row.Cells[3].AddParagraph().AppendText($"R{rowTotal:0.00}").CharacterFormat.FontName = "Poppins";
                }

                // Total row
                TableRow totalRow = itemsTable.Rows[totalRows - 1];
                totalRow.Cells[2].AddParagraph().AppendText("Total Amount:").CharacterFormat.Bold = true;
                totalRow.Cells[2].Paragraphs[0].Format.HorizontalAlignment = HorizontalAlignment.Right;
                totalRow.Cells[3].AddParagraph().AppendText($"R{totalAmount:0.00}").CharacterFormat.Bold = true;
                totalRow.Cells[3].Paragraphs[0].Format.HorizontalAlignment = HorizontalAlignment.Right;

                // Replace placeholder
                TextSelection placeholder = wordDoc.FindString("{{ItemsTable}}", true, true);
                if (placeholder != null)
                {
                    Paragraph para = placeholder.GetAsOneRange().OwnerParagraph;
                    Body body = para.OwnerTextBody;
                    int index = body.ChildObjects.IndexOf(para);
                    body.ChildObjects.Remove(para);
                    body.ChildObjects.Insert(index, itemsTable);
                }

                // --- Save files ---
                wordDoc.SaveToFile(tempDocxPath, FileFormat.Docx);
                wordDoc.SaveToFile(outputPdfPath, FileFormat.PDF);

                if (!File.Exists(outputPdfPath))
                    return (false, $"PDF generation failed. File not found at {outputPdfPath}", null);

                // --- Build URLs ---
                string baseUrl = "https://updatequotestatus-qfn3uqj3ya-uc.a.run.app";
                string acceptUrl = $"{baseUrl}?userId={userId}&quoteId={quotation.id}&status=Accepted&token={quotation.secureToken}";
                string declineUrl = $"{baseUrl}?userId={userId}&quoteId={quotation.id}&status=Declined&token={quotation.secureToken}";

                Console.WriteLine($"Accept URL: {acceptUrl}");
                Console.WriteLine($"Decline URL: {declineUrl}");

                // --- Build MailKit email ---
                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress("Quality Copiers", "zimkhitha.sasanti@gmail.com"));
                emailMessage.To.Add(new MailboxAddress(quotation.clientName, quotation.email));
                emailMessage.Subject = $"Quotation {quotation.quoteNumber}";

                var builder = new BodyBuilder();

                var htmlBodyPart = new TextPart("html")
                {
                    Text = $@"
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
                        <p>Kind Regards,<br/>Quality Copiers</p>"
                };


                // Create the PDF attachment
                var pdfAttachment = new MimePart("application", "pdf")
                {
                    Content = new MimeContent(File.OpenRead(outputPdfPath)),
                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    FileName = Path.GetFileName(outputPdfPath)
                };

                // Combine HTML + PDF in multipart/mixed
                var multipart = new Multipart("mixed");
                multipart.Add(htmlBodyPart);
                multipart.Add(pdfAttachment);

                emailMessage.Body = multipart;

                //Send via Gmail SMTP
                using var smtp = new MailKit.Net.Smtp.SmtpClient();
                await smtp.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync("zimkhitha.sasanti@gmail.com", "pflq gfdg xyeb pitx");
                await smtp.SendAsync(emailMessage);
                await smtp.DisconnectAsync(true);

                // Encrypt and store in Firestore
                var encrypted = new Quotation
                {
                    id = _encryptionHelper.Encrypt(quotation.id ?? string.Empty),
                    clientName = _encryptionHelper.Encrypt(quotation.clientName ?? string.Empty),
                    companyName = _encryptionHelper.Encrypt(quotation.companyName ?? string.Empty),
                    email = _encryptionHelper.Encrypt(quotation.email ?? string.Empty),
                    phone = _encryptionHelper.Encrypt(quotation.phone ?? string.Empty),
                    quoteNumber = _encryptionHelper.Encrypt(quotation.quoteNumber ?? string.Empty),
                    createdAt = quotation.createdAt,
                    amount = quotation.amount,
                    secureToken = quotation.secureToken ?? string.Empty,
                    pdfFileName = _encryptionHelper.Encrypt(quotation.pdfFileName ?? string.Empty),
                    Items = quotation.Items
                };

                if (encrypted.Items != null)
                {
                    foreach (var it in encrypted.Items)
                        it.description = _encryptionHelper.Encrypt(it.description ?? string.Empty);
                }

                await reservedDocRef.SetAsync(encrypted);
                await reservedDocRef.UpdateAsync("id", quotation.id);

                return (true, null, quotation.id);
            }
            catch (Exception ex)
            {
                return (false, $"Error adding quotation: {ex}", null);
            }
        }


        public async Task<List<Quotation>> GetQuotationsAsync()
        {
            try
            {
                string userId = "vz4maSc0vOgouOGPhtdkFzBlceK2";
                var quotesRef = _firestoreDb.Collection("users").Document(userId).Collection("quotes");
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

        public async Task DeleteQuotationAsync(string quoteId)
        {
            try
            {
                string userId = "vz4maSc0vOgouOGPhtdkFzBlceK2";
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

        public async Task<List<Invoice>> GetInvoicesAsync()
        {
            string userId = "vz4maSc0vOgouOGPhtdkFzBlceK2";
            var invoicesRef = _firestoreDb.Collection("users").Document(userId).Collection("invoices");
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
            string userId = "vz4maSc0vOgouOGPhtdkFzBlceK2";
            try
            {
                var invoiceRef = _firestoreDb.Collection("users").Document(userId).Collection("invoices").Document(id);

                var snapshot = await invoiceRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    return null; // or throw new Exception("Invoice not found");
                }

                var invoice = snapshot.ConvertTo<Invoice>();

                // Decrypt sensitive fields
                invoice.ClientName = _encryptionHelper.Decrypt(invoice.ClientName);
                invoice.CompanyName = _encryptionHelper.Decrypt(invoice.CompanyName);
                invoice.Phone = _encryptionHelper.Decrypt(invoice.Phone);
                invoice.QuoteNumber = _encryptionHelper.Decrypt(invoice.QuoteNumber ?? string.Empty);

                if (invoice.Items != null)
                {
                    foreach (var item in invoice.Items)
                    {
                        item.Description = _encryptionHelper.Decrypt(item.Description);
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

        public async Task<bool> GenerateAndSendInvoiceAsync(Invoice invoice)
        {
            try
            {
                string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Invoice", "InvoiceTemplate.docx");
                string generatedDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedInvoicePdfs");
                Directory.CreateDirectory(generatedDir);

                string safeClientName = string.IsNullOrWhiteSpace(invoice.ClientName)
                    ? "Client"
                    : string.Join("_", invoice.ClientName.Split(Path.GetInvalidFileNameChars()));
                string pdfFileName = $"Invoice {safeClientName} {invoice.InvoiceNumber}.pdf";

                string tempDocxPath = Path.Combine(generatedDir, $"{Path.GetFileNameWithoutExtension(pdfFileName)}.docx");
                string outputPdfPath = Path.Combine(generatedDir, pdfFileName);

                GenerateInvoicePdf(invoice, templatePath, tempDocxPath, outputPdfPath);
                await SendInvoiceEmailAsync(invoice, outputPdfPath);

                // Clean up DOCX
                if (File.Exists(tempDocxPath)) File.Delete(tempDocxPath);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to generate or send invoice {invoice.InvoiceNumber}: {ex.Message}");
                return false;
            }
        }


        private void GenerateInvoicePdf(Invoice invoice, string templatePath, string tempDocxPath, string outputPdfPath)
        {
            // Load template
            Document wordDoc = new Document();
            wordDoc.LoadFromFile(templatePath);

            // Helper method to replace text
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
            }

            // Replace static placeholders
            ReplaceText("{{ClientName}}", invoice.ClientName);
            ReplaceText("{{ClientEmail}}", invoice.Email);
            ReplaceText("{{InvoiceNumber}}", invoice.InvoiceNumber);

            // Handle CreatedAt properly (Firestore Timestamp → DateTime)
            DateTime invoiceDate = invoice.CreatedAt.HasValue
                ? invoice.CreatedAt.Value.ToDateTime()
                : DateTime.UtcNow;
            ReplaceText("{{InvoiceDate}}", invoiceDate.ToString("yyyy/MM/dd"));

            // Build Items Table
            Section section = wordDoc.Sections[0];
            Table itemsTable = section.AddTable(true);

            int totalRows = (invoice.Items?.Count ?? 0) + 2;
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
            for (int i = 0; i < (invoice.Items?.Count ?? 0); i++)
            {
                var item = invoice.Items[i];
                double rowTotal = item.Quantity * item.UnitPrice;
                totalAmount += rowTotal;

                TableRow row = itemsTable.Rows[i + 1];
                row.Cells[0].AddParagraph().AppendText(item.Quantity.ToString()).CharacterFormat.FontName = "Poppins";
                row.Cells[1].AddParagraph().AppendText(item.Description ?? string.Empty).CharacterFormat.FontName = "Poppins";
                row.Cells[2].AddParagraph().AppendText($"R{item.UnitPrice:0.00}").CharacterFormat.FontName = "Poppins";
                row.Cells[3].AddParagraph().AppendText($"R{rowTotal:0.00}").CharacterFormat.FontName = "Poppins";
            }

            // Total row
            TableRow totalRow = itemsTable.Rows[totalRows - 1];
            totalRow.Cells[2].AddParagraph().AppendText("Total Amount:").CharacterFormat.Bold = true;
            totalRow.Cells[2].Paragraphs[0].Format.HorizontalAlignment = HorizontalAlignment.Right;
            totalRow.Cells[3].AddParagraph().AppendText($"R{totalAmount:0.00}").CharacterFormat.Bold = true;
            totalRow.Cells[3].Paragraphs[0].Format.HorizontalAlignment = HorizontalAlignment.Right;

            // Invoice Table 
            TextSelection placeholder = wordDoc.FindString("{{ItemsTable}}", true, true);
            if (placeholder != null)
            {
                Paragraph para = placeholder.GetAsOneRange().OwnerParagraph;
                Body body = para.OwnerTextBody;
                int index = body.ChildObjects.IndexOf(para);
                body.ChildObjects.Remove(para);
                body.ChildObjects.Insert(index, itemsTable);
            }

            // Save generated files
            wordDoc.SaveToFile(tempDocxPath, FileFormat.Docx);
            wordDoc.SaveToFile(outputPdfPath, FileFormat.PDF);
        }

        private async Task SendInvoiceEmailAsync(Invoice invoice, string pdfPath)
        {
            if (invoice == null || string.IsNullOrWhiteSpace(invoice.Email) || !File.Exists(pdfPath))
                throw new ArgumentException("Invalid invoice or PDF path");

            // Build the email
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("Quality Copiers", "zimkhitha.sasanti@gmail.com"));
            emailMessage.To.Add(new MailboxAddress(invoice.ClientName, invoice.Email));
            emailMessage.Subject = $"Invoice {invoice.InvoiceNumber}";

            var builder = new BodyBuilder();

            // HTML body
            builder.HtmlBody = $@"
            <p>Dear {System.Net.WebUtility.HtmlEncode(invoice.ClientName)},</p>
            <p>Thank you for your business. Please find your invoice attached.</p>
            <p>The status of this invoice is <strong>{System.Net.WebUtility.HtmlEncode(invoice.Status)}</strong>.</p>
            <p>Kind regards,<br/>Quality Copiers</p>";

            // Attach PDF
            var pdfAttachment = new MimePart("application", "pdf")
            {
                Content = new MimeContent(File.OpenRead(pdfPath), ContentEncoding.Base64),
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                ContentTransferEncoding = ContentEncoding.Base64,
                FileName = Path.GetFileName(pdfPath)
            };

            var multipart = new Multipart("mixed");
            multipart.Add(new TextPart("html") { Text = builder.HtmlBody });
            multipart.Add(pdfAttachment);

            emailMessage.Body = multipart;

            // Send email via Gmail SMTP
            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            await smtp.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync("zimkhitha.sasanti@gmail.com", "pflq gfdg xyeb pitx");
            await smtp.SendAsync(emailMessage);
            await smtp.DisconnectAsync(true);
        }

        public async Task DeleteInvoiceAsync(string invoiceId)
        {
            try
            {
                string userId = "vz4maSc0vOgouOGPhtdkFzBlceK2";
                DocumentReference invoiceDoc = _firestoreDb
                    .Collection("users")
                    .Document(userId)
                    .Collection("invoices")
                    .Document(invoiceId);

                await invoiceDoc.DeleteAsync();
                Console.WriteLine($"Invoice {invoiceId} deleted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting quotation: {ex.Message}");
            }
        }

        public async Task MarkInvoiceAsPaidAsync(string invoiceId)
        {
            string userId = "vz4maSc0vOgouOGPhtdkFzBlceK2"; // Replace with dynamic user ID if applicable

            try
            {
                Console.WriteLine($"🔍 Trying to mark as paid - User: {userId}, Invoice: {invoiceId}");

                var invoiceRef = _firestoreDb.Collection("users")
                    .Document(userId)
                    .Collection("invoices")
                    .Document(invoiceId);

                var snapshot = await invoiceRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    Console.WriteLine($"❌ Invoice not found at path: users/{userId}/invoices/{invoiceId}");
                    throw new Exception("Invoice document does not exist in Firestore.");
                }

                await invoiceRef.UpdateAsync("status", "Paid");
                Console.WriteLine("✅ Invoice successfully marked as paid!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 Firestore update failed: " + ex.Message);
                throw;
            }
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

        public async Task UpdateManagerDataAsync(string userId, Dictionary<string, object> updatedData)
        {
            var docRef = _firestoreDb
                .Collection("users")
                .Document(userId)
                .Collection("manager_data")
                .Document(userId);

            await docRef.SetAsync(updatedData, SetOptions.MergeAll);
        }

    }
}