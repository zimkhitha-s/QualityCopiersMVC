using System.Drawing;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using INSY7315_ElevateDigitalStudios_POE.Models;
using INSY7315_ElevateDigitalStudios_POE.Models.Dtos;
using INSY7315_ElevateDigitalStudios_POE.Security;
using MimeKit;
using Spire.Doc;
using Spire.Doc.Documents;
using Spire.Doc.Fields;
using Google.Cloud.SecretManager.V1;
using System.Drawing;
using INSY7315_ElevateDigitalStudios_POE.Helper;

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
        private readonly string _firebaseProjectId;

        // firebase service constructor - initialize firestore connection and encryption helper
        public FirebaseService(EncryptionHelper encryptionHelper, MailService mailService, IConfiguration configuration)
        {
            _encryptionHelper = encryptionHelper;
            _mailService = mailService;
            _configuration = configuration;

            _firebaseProjectId = Environment.GetEnvironmentVariable("GCP_PROJECT_ID");

            var firebaseKeyJson = SecretManagerHelper.GetSecret(_firebaseProjectId, "firebase-admin-key");

            if (FirebaseApp.DefaultInstance == null)
            {
                // initialize firebase app with default credentials
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromJson(firebaseKeyJson)
                });
            }

            // initialize firestore database and encryption helper
            _firestoreDb = FirestoreDb.Create("insy7315-database2");

            _senderEmail = SecretManagerHelper.GetSecret(_firebaseProjectId, "email-smtp-user");
            _smtpPassword = SecretManagerHelper.GetSecret(_firebaseProjectId, "email-smtp-password");
        }

        public string ManagerEmail => _mailService.ManagerEmail;

        // method to get firestore database instance
        public FirestoreDb GetFirestore()
        {
            return _firestoreDb;
        }

        // method to add a new client to firestore
        public async Task AddClientAsync(Client client)
        {
            // assign unique id's to clients
            client.id = Guid.NewGuid().ToString();

            // encrypting the sensitive fields
            client.name = _encryptionHelper.Encrypt(client.name);
            client.surname = _encryptionHelper.Encrypt(client.surname);
            client.email = _encryptionHelper.Encrypt(client.email);
            client.phoneNumber = _encryptionHelper.Encrypt(client.phoneNumber);
            client.address = _encryptionHelper.Encrypt(client.address);
            client.companyName = _encryptionHelper.Encrypt(client.companyName);

            // setting created at timestamp
            var clientsRef = GetClientsCollection();

            // adding the client document
            await clientsRef.Document(client.id).SetAsync(client);
        }

        // method to get all clients for a user
        public async Task<List<Client>> GetClientsAsync()
        {
            // getting the clients collection reference
            var clientsRef = GetClientsCollection();

            // fetching all client documents
            QuerySnapshot snapshot = await clientsRef.GetSnapshotAsync();
            var clients = new List<Client>();

            // decrypting sensitive fields before returning
            foreach (var doc in snapshot.Documents)
            {
                // convert document to client object
                Client client = doc.ConvertTo<Client>();
                client.id = doc.Id;

                // decrypt sensitive fields
                client.name = _encryptionHelper.Decrypt(client.name).Trim();
                client.surname = _encryptionHelper.Decrypt(client.surname).Trim();
                client.email = _encryptionHelper.Decrypt(client.email).Trim();
                client.phoneNumber = _encryptionHelper.Decrypt(client.phoneNumber).Trim();
                client.address = _encryptionHelper.Decrypt(client.address).Trim();
                client.companyName = _encryptionHelper.Decrypt(client.companyName).Trim();

                // handle created at safely
                if (client.createdAt is Timestamp ts)
                    client.createdAtDateTime = ts.ToDateTime();
                else if (client.createdAt is long unixTimestamp)
                    client.createdAtDateTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
                else
                    client.createdAtDateTime = DateTime.UtcNow;

                // add to list
                clients.Add(client);
            }

            // return the list of clients
            return clients;
        }

        // get a single client by id
        public async Task<Client> GetClientByIdAsync(string clientId)
        {
            // getting the clients collection reference
            var clientsRef = GetClientsCollection();
            DocumentReference clientDocRef = clientsRef.Document(clientId);

            // fetching the client document
            DocumentSnapshot snapshot = await clientDocRef.GetSnapshotAsync();
            if (!snapshot.Exists) return null;

            // convert document to client object
            Client client = snapshot.ConvertTo<Client>();
            client.id = snapshot.Id;

            // decrypt sensitive fields
            client.name = _encryptionHelper.Decrypt(client.name).Trim();
            client.surname = _encryptionHelper.Decrypt(client.surname).Trim();
            client.email = _encryptionHelper.Decrypt(client.email).Trim();
            client.phoneNumber = _encryptionHelper.Decrypt(client.phoneNumber).Trim();
            client.address = _encryptionHelper.Decrypt(client.address).Trim()   ;
            client.companyName = _encryptionHelper.Decrypt(client.companyName).Trim();

            // handle created ar safely
            if (client.createdAt is Timestamp ts)
                client.createdAtDateTime = ts.ToDateTime();
            else if (client.createdAt is long unixTimestamp)
                client.createdAtDateTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
            else
                client.createdAtDateTime = DateTime.UtcNow;

            // return the client object
            return client;
        }

        // method to update an existing client
        public async Task UpdateClientAsync(ClientUpdateDto dto)
        {
            // getting the clients collection reference
            var clientsRef = GetClientsCollection();
            DocumentReference clientDocRef = clientsRef.Document(dto.Id);

            // fetching the client document 
            DocumentSnapshot snapshot = await clientDocRef.GetSnapshotAsync();
            if (!snapshot.Exists) throw new Exception("Client not found");

            // convert document to client object
            Client client = snapshot.ConvertTo<Client>();

            // split full name into name + surname if provided
            if (!string.IsNullOrWhiteSpace(dto.FullName))
            {
                var nameParts = dto.FullName.Trim().Split(' ', 2);
                if (nameParts.Length > 0)
                    client.name = _encryptionHelper.Encrypt(nameParts[0]);
                if (nameParts.Length > 1)
                    client.surname = _encryptionHelper.Encrypt(nameParts[1]);
            }

            // update only the provided fields
            if (!string.IsNullOrWhiteSpace(dto.CompanyName))
                client.companyName = _encryptionHelper.Encrypt(dto.CompanyName);

            if (!string.IsNullOrWhiteSpace(dto.Email))
                client.email = _encryptionHelper.Encrypt(dto.Email);

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                client.phoneNumber = _encryptionHelper.Encrypt(dto.PhoneNumber);

            if (!string.IsNullOrWhiteSpace(dto.Address))
                client.address = _encryptionHelper.Encrypt(dto.Address);

            // save the updated client document
            await clientDocRef.SetAsync(client, SetOptions.Overwrite);
        }

        // method to delete a client by id
        public async Task DeleteClientAsync(string clientId)
        {
            try
            {
                // getting the clients collection reference
                var clientsRef = GetClientsCollection();
                DocumentReference clientDocRef = clientsRef.Document(clientId);

                // deleting the client document
                await clientDocRef.DeleteAsync();
                Console.WriteLine($"Client {clientId} deleted successfully.");
            }
            catch (Exception ex)
            {
                // log the error
                Console.WriteLine($"Error deleting client: {ex.Message}");
            }
        }

        // helper method to get clients collection reference
        private CollectionReference GetClientsCollection()
        {
            return _firestoreDb.Collection("clients");
        }

        // method to add a new employee
        public async Task<(bool Success, string ErrorMessage, string TempPassword)> AddEmployeeAsync(Employee employee)
        {
            try
            {
                // input validation
                string managerUid = "daMmNRUlirZSsh4zC1c3N7AtqCG2";

                // generate a temp password for the employees - they can change it later
                employee.Password = employee.IdNumber.Substring(employee.IdNumber.Length - 6) + "@QC";
                employee.Role = "Employee";

                // create user in firebase authentication
                var userRecordArgs = new UserRecordArgs
                {
                    Email = employee.Email,
                    EmailVerified = false,
                    Password = employee.Password,
                    DisplayName = employee.FullName,
                    Disabled = false
                };

                // create the user in firebase auth
                UserRecord newUser = await FirebaseAuth.DefaultInstance.CreateUserAsync(userRecordArgs);

                // set udi and encrypt fields
                employee.Uid = newUser.Uid;

                // encrypt sensitive fields
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

                // store the encrypted employee in firestore
                var employeesRef = GetEmployeesCollection(managerUid);
                await employeesRef.Document(encryptedEmployee.Uid).SetAsync(encryptedEmployee);

                // return success with temp password
                return (true, null, employee.Password);
            }
            catch (FirebaseAuthException ex)
            {
                // handle firebase auth errors
                return (false, $"Firebase Auth Error: {ex.Message}", null);
            }
            catch (Exception ex)
            {
                // handle general errors
                return (false, $"Error adding employee: {ex.Message}", null);
            }
        }

        // method to get all employees
        public async Task<List<Employee>> GetAllEmployeesAsync()
        {
            try
            {
                // getting the employees collection reference
                var employeesRef = GetEmployeesCollection();

                // fetching all employee documents
                QuerySnapshot snapshot = await employeesRef.GetSnapshotAsync();

                // decrypting sensitive fields before returning
                List<Employee> employees = new List<Employee>();

                // decryption loop
                foreach (var doc in snapshot.Documents)
                {
                    // convert document to employee object
                    Employee employee = doc.ConvertTo<Employee>();

                    // decrypt sensitive fields
                    employee.Name = _encryptionHelper.Decrypt(employee.Name);
                    employee.Surname = _encryptionHelper.Decrypt(employee.Surname);
                    employee.FullName = _encryptionHelper.Decrypt(employee.FullName ?? string.Empty);
                    employee.IdNumber = _encryptionHelper.Decrypt(employee.IdNumber);
                    employee.Email = _encryptionHelper.Decrypt(employee.Email);
                    employee.PhoneNumber = _encryptionHelper.Decrypt(employee.PhoneNumber);
                    employee.Role = _encryptionHelper.Decrypt(employee.Role);

                    // convert created at to created at date time for display
                    if (employee.CreatedAt != null)
                    {
                        if (employee.CreatedAt is Timestamp ts)
                            employee.CreatedAtDateTime = ts.ToDateTime();
                        else if (employee.CreatedAt is DateTime dt)
                            employee.CreatedAtDateTime = dt;
                        else
                            employee.CreatedAtDateTime = DateTime.UtcNow;
                    }

                    // add to list
                    employees.Add(employee);
                }

                // return the list of employees
                return employees;
            }
            catch (Exception ex)
            {
                // handle errors
                throw new Exception($"Error fetching employees: {ex.Message}");
            }
        }

        // method to get a single employee by id
        public async Task<Employee> GetEmployeeByIdAsync(string employeeId)
        {
            // getting the employees collection reference
            var employeesRef = GetEmployeesCollection();
            DocumentReference employeeDocRef = employeesRef.Document(employeeId);

            // fetching the employee document
            DocumentSnapshot snapshot = await employeeDocRef.GetSnapshotAsync();
            if (!snapshot.Exists) return null;

            // convert document to employee object
            Employee employee = snapshot.ConvertTo<Employee>();
            employee.Uid = snapshot.Id;

            // decrypt sensitive fields
            employee.Name = _encryptionHelper.Decrypt(employee.Name).Trim();
            employee.Surname = _encryptionHelper.Decrypt(employee.Surname).Trim();
            employee.Email = _encryptionHelper.Decrypt(employee.Email).Trim();
            employee.PhoneNumber = _encryptionHelper.Decrypt(employee.PhoneNumber).Trim();

            // handle created at safely
            if (employee.CreatedAt is Timestamp ts)
                employee.CreatedAtDateTime = ts.ToDateTime();
            else if (employee.CreatedAt is long unixTimestamp)
                employee.CreatedAtDateTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
            else
                employee.CreatedAtDateTime = DateTime.UtcNow;

            // return the employee object
            return employee;
        }

        // method to update an existing employee
        public async Task UpdateEmployeeAsync(EmployeeUpdateDto dto)
        {
            // getting the employees collection reference
            var employeesRef = GetEmployeesCollection();
            DocumentReference employeeDocRef = employeesRef.Document(dto.Id);

            // fetching the employee document
            DocumentSnapshot snapshot = await employeeDocRef.GetSnapshotAsync();
            if (!snapshot.Exists) throw new Exception("Employee not found");

            // convert document to employee object
            Employee employee = snapshot.ConvertTo<Employee>();

            // split full name into first and last name if provided
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

            // update phone number if provided
            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                employee.PhoneNumber = _encryptionHelper.Encrypt(dto.PhoneNumber);

            // save the updated employee document
            await employeeDocRef.SetAsync(employee, SetOptions.Overwrite);
        }

        // method to delete an employee by id
        public async Task DeleteEmployeeAsync(string employeeId)
        {
            try
            {
                // getting the employees collection reference
                var employeesRef = GetEmployeesCollection();
                DocumentReference employeeDocRef = employeesRef.Document(employeeId);

                // deleting the employee document
                await employeeDocRef.DeleteAsync();
                Console.WriteLine($"Employee {employeeId} deleted successfully.");
            }
            catch (Exception ex)
            {
                // log the error
                Console.WriteLine($"Error deleting employee: {ex.Message}");
                throw;
            }
        }

        // helper method to get employees collection reference
        private CollectionReference GetEmployeesCollection(string userId = "daMmNRUlirZSsh4zC1c3N7AtqCG2")
        {
            return _firestoreDb.Collection("users").Document(userId).Collection("employees");
        }

        // method to add a new quotation
        public async Task<(bool Success, string ErrorMessage, string QuoteId)> AddQuotationAsync(Quotation quotation)
        {
            try
            {
                // input validation
                if (quotation == null)
                    return (false, "Quotation was null", null);

                // prepare quotation metadata and totals
                await PrepareQuotationMetadataAsync(quotation);
                CalculateQuotationTotals(quotation);

                // generate pdf
                string outputPdfPath = await GenerateQuotationPdfAsync(quotation);

                // verify pdf was created
                if (!File.Exists(outputPdfPath))
                    return (false, $"PDF generation failed at {outputPdfPath}", null);

                // send quotation email
                await SendQuotationEmailAsync(quotation, outputPdfPath);

                // encrypt and store in the database 
                await SaveQuotationToFirestoreAsync(quotation);

                // return success with quote id
                return (true, null, quotation.id);
            }
            catch (Exception ex)
            {
                // handle general errors
                return (false, $"Error adding quotation: {ex.Message}", null);
            }
        }

        // method to save quotation to firestore
        private async Task PrepareQuotationMetadataAsync(Quotation quotation)
        {
            // get reference to the shared firestore counter - quote number
            var counterRef = _firestoreDb.Collection("settings").Document("quote_counter");

            // increment the counter using the firestore transaction
            long nextQuoteNumber = await _firestoreDb.RunTransactionAsync(async transaction =>
            {
                // get the current snapshot of the counter document 
                var snapshot = await transaction.GetSnapshotAsync(counterRef);

                // read the last quote number or initialize if not present
                long lastNumber = snapshot.ContainsField("lastQuoteNumber")
                    ? snapshot.GetValue<long>("lastQuoteNumber")
                    : 10000;

                // calculate the next quote number
                long nextNumber = lastNumber + 1;

                // update the counter document with the new last quote number
                transaction.Update(counterRef, "lastQuoteNumber", nextNumber);
                return nextNumber;
            });

            // create the quotation with the meta data
            var quotesRef = _firestoreDb.Collection("quotes");
            var reservedDocRef = quotesRef.Document();

            // assign metadata to the quotation
            quotation.id = reservedDocRef.Id;
            quotation.quoteNumber = $"#{nextQuoteNumber}";
            quotation.createdAt = Timestamp.FromDateTime(DateTime.UtcNow);
            quotation.secureToken = Guid.NewGuid().ToString("N");
        }

        // method to save quotation to firestore
        private async Task<long> GetNextQuoteNumberAsync()
        {
            // get reference to the shared firestore counter - quote number
            var settingsRef = _firestoreDb.Collection("settings").Document("quote_counter");

            // increment the counter using the firestore transaction
            return await _firestoreDb.RunTransactionAsync(async transaction =>
            {
                // get the current snapshot of the counter document
                var snapshot = await transaction.GetSnapshotAsync(settingsRef);
                long lastNumber = snapshot.ContainsField("lastQuoteNumber")
                    ? snapshot.GetValue<long>("lastQuoteNumber")
                    : 10000;

                // calculate the next quote number
                long nextNumber = lastNumber + 1;
                transaction.Update(settingsRef, "lastQuoteNumber", nextNumber);
                return nextNumber;
            });
        }

        // method to save quotation to firestore
        private void CalculateQuotationTotals(Quotation quotation)
        {
            // calculate totals for each item and overall quotation
            double total = 0.0;
            if (quotation.Items == null) return;

            // loop through each item
            foreach (var item in quotation.Items)
            {
                // safeguard against null items
                if (item == null) continue;
                item.quantity = Math.Max(item.quantity, 0);
                item.unitPrice = Math.Max(item.unitPrice, 0);
                item.amount = item.quantity * item.unitPrice;
                total += item.amount;
            }

            // assign total to quotation
            quotation.amount = total;
        }

        // method to save quotation to firestore
        private async Task<string> GenerateQuotationPdfAsync(Quotation quotation)
        {
            // sanitize client name for filename
            string safeClientName = string.IsNullOrWhiteSpace(quotation.clientName)
                ? "Client"
                : string.Join("_", quotation.clientName.Split(Path.GetInvalidFileNameChars()));

            quotation.pdfFileName = $"Quotation_{safeClientName}_{quotation.quoteNumber}.pdf";

            // load the Word template
            string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Quotation", "QCQuotationsTemplate.docx");
            string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedQuotationPdfs");
            Directory.CreateDirectory(outputDir);

            // define output paths
            string docxPath = Path.Combine(outputDir, $"{Path.GetFileNameWithoutExtension(quotation.pdfFileName)}.docx");
            string pdfPath = Path.Combine(outputDir, quotation.pdfFileName);

            // load the Word document
            Document wordDoc = new Document();
            wordDoc.LoadFromFile(templatePath);

            // replace placeholders with proper values
            ReplaceText(wordDoc, "{{ClientName}}", quotation.clientName);
            ReplaceText(wordDoc, "{{CompanyName}}", quotation.companyName);
            ReplaceText(wordDoc, "{{Address}}", quotation.address);
            ReplaceText(wordDoc, "{{ClientEmail}}", quotation.email);
            ReplaceText(wordDoc, "{{PhoneNumber}}", quotation.phone);
            ReplaceText(wordDoc, "{{QuoteNumber}}", quotation.quoteNumber ?? string.Empty);
            ReplaceText(wordDoc, "{{QuoteDate}}", quotation.createdAt.ToDateTime().ToString("yyyy-MM-dd"));

            // insert item table
            InsertItemsTable(wordDoc, quotation);

            // save to files
            wordDoc.SaveToFile(docxPath, FileFormat.Docx);
            wordDoc.SaveToFile(pdfPath, FileFormat.PDF);

            // cleanup
            return pdfPath;
        }

        // method to replace text in the document
        private void ReplaceText(Document wordDoc, string placeholder, string value)
        {
            // find and replace the placeholder with the actual value
            var selection = wordDoc.FindString(placeholder, true, true);
            if (selection != null)
            {
                // replace text and set formatting
                var range = selection.GetAsOneRange();
                range.Text = value ?? string.Empty;
                range.CharacterFormat.FontName = "Century Gothic";
                range.CharacterFormat.FontSize = 11;
            }
        }

        // method to insert items table into the document
        private void InsertItemsTable(Document wordDoc, Quotation quotation)
        {
            // get the first section of the document
            var section = wordDoc.Sections[0];
            int itemCount = quotation.Items?.Count ?? 0;
            int totalRows = itemCount + 2;

            // define blue color
            var blue = Color.FromArgb(26, 46, 99);

            // create clean, full-width table
            Table table = section.AddTable(true);
            table.ResetCells(totalRows, 4);
            table.TableFormat.Paddings.All = 5f;
            table.TableFormat.HorizontalAlignment = RowAlignment.Left;
            table.TableFormat.Borders.BorderType = BorderStyle.None;
            table.PreferredWidth = new PreferredWidth(WidthType.Percentage, 100);

            // remove all borders 
            foreach (TableRow r in table.Rows)
            {
                r.RowFormat.Borders.BorderType = BorderStyle.None;
                foreach (TableCell c in r.Cells)
                {
                    c.CellFormat.Borders.BorderType = BorderStyle.None;
                }
            }

            // definging column widths
            table.Rows[0].Cells[0].Width = 60; 
            table.Rows[0].Cells[1].Width = 320;
            table.Rows[0].Cells[2].Width = 120;
            table.Rows[0].Cells[3].Width = 120;

            // header row
            string[] headers = { "Qty", "Description", "Unit Price", "Amount" };
            var headerRow = table.Rows[0];
            headerRow.HeightType = TableRowHeightType.AtLeast;
            headerRow.Height = 22f;

            // headwer formatting - blue + bold + underline
            for (int i = 0; i < headers.Length; i++)
            {
                // header text
                Paragraph p = headerRow.Cells[i].AddParagraph();
                TextRange tr = p.AppendText(headers[i]);
                tr.CharacterFormat.FontName = "Century Gothic";
                tr.CharacterFormat.FontSize = 11;
                tr.CharacterFormat.Bold = true;
                tr.CharacterFormat.TextColor = blue;

                // alignment
                if (i == 0) p.Format.HorizontalAlignment = HorizontalAlignment.Center;
                else if (i == 1) p.Format.HorizontalAlignment = HorizontalAlignment.Left;
                else p.Format.HorizontalAlignment = HorizontalAlignment.Right;

                // add blue horizontal line under header
                headerRow.Cells[i].CellFormat.Borders.Bottom.BorderType = BorderStyle.Single;
                headerRow.Cells[i].CellFormat.Borders.Bottom.Color = blue;
                headerRow.Cells[i].CellFormat.Borders.Bottom.LineWidth = 1.0f;

                // remove vertical lines
                headerRow.Cells[i].CellFormat.Borders.Left.BorderType = BorderStyle.None;
                headerRow.Cells[i].CellFormat.Borders.Right.BorderType = BorderStyle.None;
            }

            // item rows
            double total = 0;
            for (int i = 0; i < itemCount; i++)
            {
                // current item and row
                var it = quotation.Items[i];
                var row = table.Rows[i + 1];
                row.HeightType = TableRowHeightType.AtLeast;
                row.Height = 20f;

                // remove vertical lines
                foreach (TableCell c in row.Cells)
                {
                    c.CellFormat.Borders.Left.BorderType = BorderStyle.None;
                    c.CellFormat.Borders.Right.BorderType = BorderStyle.None;
                }

                // qty
                {
                    var p = row.Cells[0].AddParagraph();
                    var tr = p.AppendText(it.quantity.ToString());
                    tr.CharacterFormat.FontName = "Century Gothic";
                    tr.CharacterFormat.FontSize = 11;
                    p.Format.HorizontalAlignment = HorizontalAlignment.Center;
                }

                // description
                {
                    var p = row.Cells[1].AddParagraph();
                    var tr = p.AppendText(it.description ?? "");
                    tr.CharacterFormat.FontName = "Century Gothic";
                    tr.CharacterFormat.FontSize = 11;
                    p.Format.HorizontalAlignment = HorizontalAlignment.Left;
                }

                // unit Price
                {
                    var p = row.Cells[2].AddParagraph();
                    var tr = p.AppendText($"R{it.unitPrice:0.00}");
                    tr.CharacterFormat.FontName = "Century Gothic";
                    tr.CharacterFormat.FontSize = 11;
                    p.Format.HorizontalAlignment = HorizontalAlignment.Right;
                }

                // amount
                {
                    var p = row.Cells[3].AddParagraph();
                    var tr = p.AppendText($"R{it.amount:0.00}");
                    tr.CharacterFormat.FontName = "Century Gothic";
                    tr.CharacterFormat.FontSize = 11;
                    p.Format.HorizontalAlignment = HorizontalAlignment.Right;
                }

                total += it.amount;
            }

            // total row
            var totalRow = table.Rows[totalRows - 1];
            totalRow.HeightType = TableRowHeightType.AtLeast;
            totalRow.Height = 24f;

            // adding blue line above TOTAL
            for (int i = 0; i < 4; i++)
            {
                totalRow.Cells[i].CellFormat.Borders.Top.BorderType = BorderStyle.Single;
                totalRow.Cells[i].CellFormat.Borders.Top.Color = blue;
                totalRow.Cells[i].CellFormat.Borders.Top.LineWidth = 1.0f;

                totalRow.Cells[i].CellFormat.Borders.Left.BorderType = BorderStyle.None;
                totalRow.Cells[i].CellFormat.Borders.Right.BorderType = BorderStyle.None;
            }

            // empty spacing for first two cells
            totalRow.Cells[0].AddParagraph().AppendText("");
            totalRow.Cells[1].AddParagraph().AppendText("");

            // TOTAL label - blue and bold
            {
                Paragraph p = totalRow.Cells[2].AddParagraph();
                TextRange tr = p.AppendText("TOTAL:");
                tr.CharacterFormat.FontName = "Century Gothic";
                tr.CharacterFormat.FontSize = 11;
                tr.CharacterFormat.Bold = true;
                tr.CharacterFormat.TextColor = blue;
                p.Format.HorizontalAlignment = HorizontalAlignment.Right;
            }

            // Amount - bold black
            {
                Paragraph p = totalRow.Cells[3].AddParagraph();
                TextRange tr = p.AppendText($"R{total:0.00}");
                tr.CharacterFormat.FontName = "Century Gothic";
                tr.CharacterFormat.FontSize = 11;
                tr.CharacterFormat.Bold = true;
                tr.CharacterFormat.TextColor = Color.Black;
                p.Format.HorizontalAlignment = HorizontalAlignment.Right;
            }

            // replace the table placeholder
            var placeholder = wordDoc.FindString("{{ItemTable}}", true, true);
            if (placeholder != null)
            {
                var para = placeholder.GetAsOneRange().OwnerParagraph;
                var body = para.OwnerTextBody;
                int idx = body.ChildObjects.IndexOf(para);
                body.ChildObjects.Remove(para);
                body.ChildObjects.Insert(idx, table);
            }
        }

        // method to send quotation email
        private async Task SendQuotationEmailAsync(Quotation quotation, string pdfPath)
        {
            // prepare acceptance and decline links
            string userId = "daMmNRUlirZSsh4zC1c3N7AtqCG2";

            // base url for updating quote status
            string baseUrl = "https://us-central1-insy7315-database2.cloudfunctions.net/updateQuoteStatus";

            // construct full status urls with query parameters - userId, quoteId, status, token
            string acceptUrl = $"{baseUrl}?userId={userId}&quoteId={quotation.id}&status=Accepted&token={quotation.secureToken}";
            string declineUrl = $"{baseUrl}?userId={userId}&quoteId={quotation.id}&status=Declined&token={quotation.secureToken}";

            // compose the email
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("Quality Copiers", ManagerEmail));
            email.To.Add(new MailboxAddress(quotation.clientName, quotation.email));
            email.Subject = $"Quotation {quotation.quoteNumber}";

            // build the email body with HTML and attachment
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

        // method to save quotation to firestore
        private async Task SaveQuotationToFirestoreAsync(Quotation quotation)
        {
            // encrypt sensitive fields before storing
            var quotesRef = _firestoreDb.Collection("quotes");
            var docRef = quotesRef.Document(quotation.id);

            // create encrypted quotation object
            var encrypted = new Quotation
            {
                id = quotation.id ?? "",
                clientName = _encryptionHelper.Encrypt(quotation.clientName ?? ""),
                companyName = _encryptionHelper.Encrypt(quotation.companyName ?? ""),
                email = _encryptionHelper.Encrypt(quotation.email ?? ""),
                phone = _encryptionHelper.Encrypt(quotation.phone ?? ""),
                quoteNumber = _encryptionHelper.Encrypt(quotation.quoteNumber ?? ""),
                address = _encryptionHelper.Encrypt(quotation.address ?? ""),
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

            // save the encrypted quotation
            await docRef.SetAsync(encrypted);
            await docRef.UpdateAsync("id", quotation.id);
        }

        // method to get all quotations
        public async Task<List<Quotation>> GetQuotationsAsync()
        {
            try
            {
                // fetch all quotations from firestore
                var quotesRef = _firestoreDb.Collection("quotes");
                var snapshot = await quotesRef.GetSnapshotAsync();

                // decrypt sensitive fields before returning
                List<Quotation> quotations = new();

                // decryption loop
                foreach (var doc in snapshot.Documents)
                {
                    if (!doc.Exists) continue;

                    var quotation = doc.ConvertTo<Quotation>();

                    // decrypt top-level sensitive fields
                    quotation.quoteNumber = _encryptionHelper.Decrypt(quotation.quoteNumber ?? string.Empty);
                    quotation.clientName = _encryptionHelper.Decrypt(quotation.clientName);
                    quotation.companyName = _encryptionHelper.Decrypt(quotation.companyName);
                    quotation.email = _encryptionHelper.Decrypt(quotation.email);
                    quotation.phone = _encryptionHelper.Decrypt(quotation.phone);
                    quotation.address = _encryptionHelper.Decrypt(quotation.address);

                    // decrypt item descriptions
                    if (quotation.Items != null)
                    {
                        foreach (var item in quotation.Items)
                        {
                            if (item == null) continue;
                            item.description = _encryptionHelper.Decrypt(item.description);
                        }
                    }

                    // add to list
                    quotations.Add(quotation);
                }

                // return the list of quotations
                return quotations;
            }
            catch (Exception ex)
            {
                // log the error
                Console.WriteLine($"Error fetching quotations: {ex.Message}");
                return new List<Quotation>();
            }
        }

        // generating quotation pdf bytes - used for downloading/saving quotation
        public async Task<(byte[] PdfBytes, string PdfFileName)> GenerateQuotationPdfBytesAsync(Quotation quotation)
        {
            // validate the quotation
            if (quotation == null || quotation.Items == null || !quotation.Items.Any())
                throw new ArgumentException("Quotation is invalid");

            // generate the pdf and get the path
            string pdfPath = await GenerateQuotationPdfAsync(quotation);

            if (!File.Exists(pdfPath))
                throw new FileNotFoundException("Failed to generate quotation PDF", pdfPath);

            // read the pdf bytes
            byte[] pdfBytes = await File.ReadAllBytesAsync(pdfPath);

            // clean up docx 
            string docxPath = Path.Combine(Path.GetDirectoryName(pdfPath), Path.GetFileNameWithoutExtension(pdfPath) + ".docx");
            if (File.Exists(docxPath))
                File.Delete(docxPath);

            // return bytes and filename
            return (pdfBytes, quotation.pdfFileName);
        }

        // method to delete a quotation by id
        public async Task DeleteQuotationAsync(string quoteId)
        {
            try
            {
                // get the quotations collection reference
                DocumentReference quoteDoc = GetQuotationsCollection().Document(quoteId);

                // delete the quotation document
                await quoteDoc.DeleteAsync();
                Console.WriteLine($"Quotation {quoteId} deleted successfully.");
            }
            catch (Exception ex)
            {
                // log the error
                Console.WriteLine($"Error deleting quotation: {ex.Message}");
            }
        }

        // helper method to get quotations collection reference
        private CollectionReference GetQuotationsCollection()
        {
            // return the quotations collection reference
            return _firestoreDb.Collection("quotes");
        }

        // method to get all invoices
        public async Task<List<Invoice>> GetInvoicesAsync()
        {
            // fetch all invoices from firestore
            var invoicesRef = GetInvoicesCollection();
            var snapshot = await invoicesRef.GetSnapshotAsync();

            // decrypt sensitive fields before returning
            var invoices = new List<Invoice>();

            // decryption loop
            foreach (var doc in snapshot.Documents)
            {
                var invoice = doc.ConvertTo<Invoice>();

                // decrypt sensitive fields
                invoice.ClientName = _encryptionHelper.Decrypt(invoice.ClientName);
                invoice.CompanyName = _encryptionHelper.Decrypt(invoice.CompanyName);
                invoice.Email = _encryptionHelper.Decrypt(invoice.Email);
                invoice.Phone = _encryptionHelper.Decrypt(invoice.Phone);
                invoice.Address = _encryptionHelper.Decrypt(invoice.Address ?? string.Empty);
                invoice.QuoteNumber = _encryptionHelper.Decrypt(invoice.QuoteNumber ?? string.Empty);
                invoice.Status = invoice.Status;

                if (invoice.Items != null)
                {
                    foreach (var item in invoice.Items)
                    {
                        item.Description = _encryptionHelper.Decrypt(item.Description);
                    }
                }

                // add to list
                invoices.Add(invoice);
            }

            // return the list of invoices
            return invoices;
        }

        // method to get all invoices
        /*public async Task<List<Invoice>> GetPaymentsAsync()
        {
            // fetch all invoices from firestore
            var paymentsRef = GetPaymentsCollection();
            var snapshot = await paymentsRef.GetSnapshotAsync();

            // decrypt sensitive fields before returning
            var payments = new List<Payment>();

            // decryption loop
            foreach (var doc in snapshot.Documents)
            {
                var payment = doc.ConvertTo<Payment>();

                // decrypt sensitive fields
                payment.ClientName = _encryptionHelper.Decrypt(payment.ClientName);
                payment.CompanyName = _encryptionHelper.Decrypt(payment.CompanyName);
                payment.Email = _encryptionHelper.Decrypt(payment.Email);
                payment.Phone = _encryptionHelper.Decrypt(payment.Phone);
                payment.Address = _encryptionHelper.Decrypt(payment.Address ?? string.Empty);
                payment.QuoteNumber = _encryptionHelper.Decrypt(payment.QuoteNumber ?? string.Empty);
                payment.Status = payment.Status;

                if (payment.Items != null)
                {
                    foreach (var item in payment.Items)
                    {
                        item.Description = _encryptionHelper.Decrypt(item.Description);
                    }
                }

                // add to list
                payments.Add(payment);
            }

            // return the list of invoices
            return payments;
        }*/

        // get invoice details by id for payments
        public async Task<Invoice?> GetInvoiceDetailsAsync(string id)
        {
            try
            {
                // fetch the invoice document by id
                var invoiceRef = GetInvoicesCollection().Document(id);
                var snapshot = await invoiceRef.GetSnapshotAsync();

                // check if invoice exists
                if (!snapshot.Exists)
                    return null;

                // convert document to invoice object
                var invoice = snapshot.ConvertTo<Invoice>();

                // decrypt sensitive fields
                invoice.ClientName = _encryptionHelper.Decrypt(invoice.ClientName);
                invoice.CompanyName = _encryptionHelper.Decrypt(invoice.CompanyName);
                invoice.Email = _encryptionHelper.Decrypt(invoice.Email ?? string.Empty);
                invoice.Phone = _encryptionHelper.Decrypt(invoice.Phone ?? string.Empty);
                invoice.Address = _encryptionHelper.Decrypt(invoice.Address ?? string.Empty);
                invoice.QuoteNumber = _encryptionHelper.Decrypt(invoice.QuoteNumber ?? string.Empty);

                // decrypt item descriptions
                if (invoice.Items != null)
                {
                    foreach (var item in invoice.Items)
                    {
                        item.Description = _encryptionHelper.Decrypt(item.Description ?? string.Empty);
                    }
                }

                // return the decrypted invoice
                return invoice;
            }
            catch (Exception ex)
            {
                // log the error
                Console.WriteLine($"Error retrieving invoice: {ex.Message}");
                throw;
            }
        }

        // generating invoice pdf bytes - used for downloading/saving invoice

        public async Task<(byte[] PdfBytes, string PdfFileName)> GenerateInvoicePdfBytesAsync(Invoice invoice)
        {
            // validate the invoice
            if (invoice == null || invoice.Items == null || !invoice.Items.Any())
                throw new ArgumentException("Invoice is invalid");

            // define template path
            string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Invoice", "QCInvoiceTemplate.docx");
            if (!File.Exists(templatePath))
                throw new FileNotFoundException("Invoice template not found.", templatePath);

            // define output directory
            string generatedDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedInvoicePdfs");
            Directory.CreateDirectory(generatedDir);

            // create safe client name and unique filename
            string safeClientName = string.IsNullOrWhiteSpace(invoice.ClientName)
                ? "Client"
                : string.Join("_", invoice.ClientName.Split(Path.GetInvalidFileNameChars()));

            // unique timestamp for filename
            string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            string pdfFileName = $"Invoice_{safeClientName}_{invoice.InvoiceNumber}_{timestamp}.pdf";

            // temporary docx and final pdf paths
            string tempDocxPath = Path.Combine(generatedDir, $"{Path.GetFileNameWithoutExtension(pdfFileName)}.docx");
            string outputPdfPath = Path.Combine(generatedDir, pdfFileName);

            // generate the pdf
            GenerateInvoicePdf(invoice, templatePath, tempDocxPath, outputPdfPath);

            // validate the pdf file content
            if (!File.Exists(outputPdfPath))
                throw new FileNotFoundException("Failed to generate invoice PDF", outputPdfPath);

            // read the bytes
            var pdfBytes = await File.ReadAllBytesAsync(outputPdfPath);

            // clean up temporary files
            if (File.Exists(tempDocxPath))
                File.Delete(tempDocxPath);

            // return both the pdf bytes and filename
            return (pdfBytes, pdfFileName);
        }

        // method to generate and send invoice
        public async Task<bool> GenerateAndSendInvoiceAsync(Invoice invoice)
        {
            try
            {
                // validating invoice
                if (invoice == null)
                    throw new ArgumentNullException(nameof(invoice), "Invoice cannot be null.");
                if (invoice.Items == null || !invoice.Items.Any())
                    throw new ArgumentException("Invoice items are missing.", nameof(invoice));

                // defining the file paths
                string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Invoice", "QCInvoiceTemplate.docx");
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

                // success log
                Console.WriteLine($"Invoice {invoice.InvoiceNumber} successfully generated and sent.");
                return true;
            }
            catch (ArgumentException ex)
            {
                // validation errors
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

        // method to send invoice email
        private void GenerateInvoicePdf(Invoice invoice, string templatePath, string tempDocxPath, string outputPdfPath)
        {
            // validating inputs
            if (invoice == null) throw new ArgumentNullException(nameof(invoice));
            if (invoice.Items == null || !invoice.Items.Any())
                throw new ArgumentException("Invoice must have at least one item.", nameof(invoice));

            // loading the Word template
            Document wordDoc = new Document();
            wordDoc.LoadFromFile(templatePath);

            // replace placeholders
            void ReplaceText(string placeholder, string value)
            {
                // find and replace the placeholder
                TextSelection selection = wordDoc.FindString(placeholder, true, true);
                if (selection != null)
                {
                    // replace text and set formatting
                    TextRange range = selection.GetAsOneRange();
                    range.Text = value ?? string.Empty;
                    range.CharacterFormat.FontName = "Century Gothic";
                    range.CharacterFormat.FontSize = 11;
                }
                else
                {
                    // log missing placeholder
                    Console.WriteLine($"[DEBUG] Placeholder '{placeholder}' not found.");
                }
            }

            // replacing the fields
            ReplaceText("{{ClientName}}", invoice.ClientName);
            ReplaceText("{{CompanyName}}", invoice.CompanyName);
            ReplaceText("{{ClientEmail}}", invoice.Email);
            ReplaceText("{{Address}}", invoice.Address ?? string.Empty);
            ReplaceText("{{PhoneNumber}}", invoice.Phone);
            ReplaceText("{{InvoiceNumber}}", invoice.InvoiceNumber);
            ReplaceText("{{InvoiceDate}}", (invoice.CreatedAt?.ToDateTime() ?? DateTime.UtcNow).ToString("yyyy/MM/dd"));

            // build invoice table
            var section = wordDoc.Sections[0];
            int itemCount = invoice.Items.Count;
            int totalRows = itemCount + 2;

            var blue = Color.FromArgb(26, 46, 99);
            Table table = section.AddTable(true);
            table.ResetCells(totalRows, 4);
            table.TableFormat.Paddings.All = 5f;
            table.TableFormat.Borders.BorderType = BorderStyle.None;
            table.PreferredWidth = new PreferredWidth(WidthType.Percentage, 100);

            // remove all borders to start clean
            foreach (TableRow r in table.Rows)
            {
                r.RowFormat.Borders.BorderType = BorderStyle.None;
                foreach (TableCell c in r.Cells)
                    c.CellFormat.Borders.BorderType = BorderStyle.None;
            }

            // column widths
            table.Rows[0].Cells[0].Width = 60;
            table.Rows[0].Cells[1].Width = 320;
            table.Rows[0].Cells[2].Width = 120;
            table.Rows[0].Cells[3].Width = 120;

            // header row
            string[] headers = { "Qty", "Description", "Unit Price", "Amount" };
            var headerRow = table.Rows[0];
            for (int i = 0; i < headers.Length; i++)
            {
                Paragraph p = headerRow.Cells[i].AddParagraph();
                TextRange tr = p.AppendText(headers[i]);
                tr.CharacterFormat.FontName = "Century Gothic";
                tr.CharacterFormat.FontSize = 11;
                tr.CharacterFormat.Bold = true;
                tr.CharacterFormat.TextColor = blue;

                if (i == 0) p.Format.HorizontalAlignment = HorizontalAlignment.Center;
                else if (i == 1) p.Format.HorizontalAlignment = HorizontalAlignment.Left;
                else p.Format.HorizontalAlignment = HorizontalAlignment.Right;

                // blue underline below header
                headerRow.Cells[i].CellFormat.Borders.Bottom.BorderType = BorderStyle.Single;
                headerRow.Cells[i].CellFormat.Borders.Bottom.Color = blue;
                headerRow.Cells[i].CellFormat.Borders.Bottom.LineWidth = 1.0f;
            }

            // item rows
            double totalAmount = 0;
            for (int i = 0; i < itemCount; i++)
            {
                var item = invoice.Items[i];
                double qty = Math.Max(0, item.Quantity);
                double price = Math.Max(0, item.UnitPrice);
                double rowTotal = qty * price;
                totalAmount += rowTotal;

                var row = table.Rows[i + 1];
                foreach (TableCell c in row.Cells)
                {
                    c.CellFormat.Borders.Left.BorderType = BorderStyle.None;
                    c.CellFormat.Borders.Right.BorderType = BorderStyle.None;
                }

                // qty
                {
                    Paragraph p = row.Cells[0].AddParagraph();
                    TextRange tr = p.AppendText(qty.ToString());
                    tr.CharacterFormat.FontName = "Century Gothic";
                    tr.CharacterFormat.FontSize = 11;
                    p.Format.HorizontalAlignment = HorizontalAlignment.Center;
                }

                // description
                {
                    Paragraph p = row.Cells[1].AddParagraph();
                    TextRange tr = p.AppendText(item.Description ?? "");
                    tr.CharacterFormat.FontName = "Century Gothic";
                    tr.CharacterFormat.FontSize = 11;
                    p.Format.HorizontalAlignment = HorizontalAlignment.Left;
                }

                // unit Price
                {
                    Paragraph p = row.Cells[2].AddParagraph();
                    TextRange tr = p.AppendText($"R{price:0.00}");
                    tr.CharacterFormat.FontName = "Century Gothic";
                    tr.CharacterFormat.FontSize = 11;
                    p.Format.HorizontalAlignment = HorizontalAlignment.Right;
                }

                // amount
                {
                    Paragraph p = row.Cells[3].AddParagraph();
                    TextRange tr = p.AppendText($"R{rowTotal:0.00}");
                    tr.CharacterFormat.FontName = "Century Gothic";
                    tr.CharacterFormat.FontSize = 11;
                    p.Format.HorizontalAlignment = HorizontalAlignment.Right;
                }
            }

            // total row
            var totalRow = table.Rows[totalRows - 1];
            for (int i = 0; i < 4; i++)
            {
                totalRow.Cells[i].CellFormat.Borders.Top.BorderType = BorderStyle.Single;
                totalRow.Cells[i].CellFormat.Borders.Top.Color = blue;
                totalRow.Cells[i].CellFormat.Borders.Top.LineWidth = 1.0f;
            }

            totalRow.Cells[0].AddParagraph().AppendText("");
            totalRow.Cells[1].AddParagraph().AppendText("");

            // TOTAL Label — blue and bold
            {
                Paragraph p = totalRow.Cells[2].AddParagraph();
                TextRange tr = p.AppendText("TOTAL:");
                tr.CharacterFormat.FontName = "Century Gothic";
                tr.CharacterFormat.FontSize = 11;
                tr.CharacterFormat.Bold = true;
                tr.CharacterFormat.TextColor = blue;
                p.Format.HorizontalAlignment = HorizontalAlignment.Right;
            }

            // Amount — bold black
            {
                Paragraph p = totalRow.Cells[3].AddParagraph();
                TextRange tr = p.AppendText($"R{totalAmount:0.00}");
                tr.CharacterFormat.FontName = "Century Gothic";
                tr.CharacterFormat.FontSize = 11;
                tr.CharacterFormat.Bold = true;
                tr.CharacterFormat.TextColor = Color.Black;
                p.Format.HorizontalAlignment = HorizontalAlignment.Right;
            }

            // insert table in the item table placeholder
            var placeholder = wordDoc.FindString("{{ItemTable}}", true, true);
            if (placeholder != null)
            {
                Paragraph para = placeholder.GetAsOneRange().OwnerParagraph;
                Body body = para.OwnerTextBody;
                int index = body.ChildObjects.IndexOf(para);
                body.ChildObjects.Remove(para);
                body.ChildObjects.Insert(index, table);
                Console.WriteLine("[DEBUG] Inserted invoice table at placeholder.");
            }
            else
            {
                section.Body.ChildObjects.Add(table);
                Console.WriteLine("[DEBUG] Placeholder not found — appended table.");
            }

            // save as docx and pdf
            wordDoc.SaveToFile(tempDocxPath, FileFormat.Docx);
            wordDoc.SaveToFile(outputPdfPath, FileFormat.PDF);
        }

        // method to send invoice email
        private async Task SendInvoiceEmailAsync(Invoice invoice, string pdfPath)
        {
            // validate inputs
            if (invoice == null || string.IsNullOrWhiteSpace(invoice.Email) || !File.Exists(pdfPath))
                throw new ArgumentException("Invalid invoice or PDF path");

            // build the email
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("Quality Copiers", ManagerEmail));
            emailMessage.To.Add(new MailboxAddress(invoice.ClientName, invoice.Email));
            emailMessage.Subject = $"Invoice {invoice.InvoiceNumber}";


            // build the email body with html and attachment
            var builder = new BodyBuilder();
            builder.HtmlBody = $@"
                 <p>Dear {System.Net.WebUtility.HtmlEncode(invoice.ClientName)},</p>
                 <p>Thank you for your business. Please find your invoice attached.</p>
                 <p>The status of this invoice is <strong>{System.Net.WebUtility.HtmlEncode(invoice.Status)}</strong>.</p>
                 <p>Kind regards,<br/>Quality Copiers</p>";

            // attach pdf using a fileStream kept open for the send operation
            using (var pdfStream = File.OpenRead(pdfPath))
            {
                // use mime part with mime content constructed from the stream
                var pdfAttachment = new MimePart("application", "pdf")
                {
                    Content = new MimeContent(pdfStream, ContentEncoding.Default),
                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    FileName = Path.GetFileName(pdfPath)
                };

                // create multipart/mixed to hold html and attachment
                var multipart = new Multipart("mixed");
                multipart.Add(new TextPart("html") { Text = builder.HtmlBody });
                multipart.Add(pdfAttachment);

                // set the email body
                emailMessage.Body = multipart;

                // send the email
                await _mailService.SendEmailAsync(emailMessage);
            }
        }

        // method to update invoice status
        public async Task UpdateInvoiceStatusAsync(string invoiceId, string status)
        {
            // access the invoice document inside the user's invoices subcollection
            var docRef = GetInvoicesCollection().Document(invoiceId);

            // update the status field
            var updates = new Dictionary<string, object>
            {
                { "status", status }
            };

            // perform the update
            await docRef.UpdateAsync(updates);
            if (status == "Paid")
            {
                await AddPaymentRecordAsync(invoiceId);
            }
        }

        // method to delete an invoice by id
        public async Task DeleteInvoiceAsync(string invoiceId)
        {
            try
            {
                // get the invoices collection reference
                DocumentReference invoiceDoc = GetInvoicesCollection().Document(invoiceId);

                // delete the invoice document
                await invoiceDoc.DeleteAsync();
                Console.WriteLine($"Invoice {invoiceId} deleted successfully.");
            }
            catch (Exception ex)
            {
                // log the error
                Console.WriteLine($"Error deleting quotation: {ex.Message}");
            }
        }

        // helper method to get invoices collection reference
        private CollectionReference GetInvoicesCollection()
        {
            return _firestoreDb.Collection("invoices");
        }

        // method to get manager data
        public async Task<Dictionary<string, object>> GetManagerDataAsync(string userId)
        {
            // access the manager data document for the user
            var docRef = _firestoreDb
                .Collection("users")
                .Document(userId)
                .Collection("manager_data")
                .Document(userId);

            // fetch the document snapshot
            var snapshot = await docRef.GetSnapshotAsync();

            // check if document exists and return data
            if (snapshot.Exists)
                return snapshot.ToDictionary();
            else
                throw new Exception("User data not found in Firestore.");
        }

        // method to update manager data
        public async Task<(bool Success, string Message)> UpdateManagerDataAsync(string userId, Dictionary<string, object> updatedData)
        {
            // input validation
            if (string.IsNullOrEmpty(userId))
                return (false, "User ID cannot be null or empty.");

            //  check for empty update data
            if (updatedData == null || updatedData.Count == 0)
                return (false, "No update data provided.");

            try
            {
                // reference to the manager data document
                DocumentReference docRef = _firestoreDb.Collection("users").Document(userId).Collection("manager_data").Document(userId);

                // add a timestamp to track last update
                updatedData["lastUpdated"] = Timestamp.GetCurrentTimestamp();

                // merge ensures we only update provided fields
                await docRef.SetAsync(updatedData, SetOptions.MergeAll);

                // success response
                return (true, "Profile updated successfully.");
            }
            catch (Grpc.Core.RpcException grpcEx)
            {
                // log and return firestore rpc errors
                Console.WriteLine($"Firestore RPC error for user {userId}: {grpcEx.Status.Detail}");
                return (false, $"Firestore RPC error: {grpcEx.Status.Detail}");
            }
            catch (Exception ex)
            {
                // log and return unexpected errors
                Console.WriteLine($"Firestore update failed for user {userId}: {ex.Message}");
                return (false, $"Unexpected error updating Firestore: {ex.Message}");
            }
        }

        // method to get user details with decryption
        public async Task<Dictionary<string, object>> GetUserDetailsAsync(string userId)
        {
            // access the user data document for the user
            string userDocument = "daMmNRUlirZSsh4zC1c3N7AtqCG2";

            // fetch the document snapshot
            var docRef = _firestoreDb
                .Collection("users")
                .Document(userDocument)
                .Collection("employees")
                .Document(userId);

            // snapshot retrieval
            var snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists)
                throw new Exception("User data not found in Firestore.");

            // decrypt each field in the document
            var encryptedData = snapshot.ToDictionary();
            var decryptedData = new Dictionary<string, object>();

            foreach (var kvp in encryptedData)
            {
                try
                {
                    // only decrypt string values
                    if (kvp.Value is string encryptedValue)
                    {
                        decryptedData[kvp.Key] = _encryptionHelper.Decrypt(encryptedValue);
                    }
                    else
                    {
                        // keeping non-string fields -  date time, bool, numbers as is
                        decryptedData[kvp.Key] = kvp.Value;
                    }
                }
                catch
                {
                    // if decryption fails - store the original value
                    decryptedData[kvp.Key] = kvp.Value;
                }
            }

            // return the decrypted data
            return decryptedData;
        }

        // method to update user details with encryption
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
                    .Collection("employees")
                    .Document(userId);

                // encrypting all data 
                var encryptedData = new Dictionary<string, object>();
                foreach (var entry in updatedData)
                {
                    // preserve lastUpdated field as is
                    if (entry.Key.Equals("lastUpdated", StringComparison.OrdinalIgnoreCase))
                    {
                        encryptedData[entry.Key] = entry.Value;
                    }
                    else
                    {
                        // encrypt only string values and preserve non-string
                        if (entry.Value is string strValue)
                            encryptedData[entry.Key] = _encryptionHelper.Encrypt(strValue);
                        else
                            encryptedData[entry.Key] = entry.Value;
                    }
                }

                // add or overwrite the last updated field
                encryptedData["lastUpdated"] = Timestamp.GetCurrentTimestamp();

                // merge ensures only provided fields are updated
                await docRef.SetAsync(encryptedData, SetOptions.MergeAll);

                // success response
                return (true, "Profile updated successfully.");
            }
            catch (Grpc.Core.RpcException grpcEx)
            {
                // log and return firestore rpc errors
                Console.WriteLine($"Firestore RPC error for user {userId}: {grpcEx.Status.Detail}");
                return (false, $"Firestore RPC error: {grpcEx.Status.Detail}");
            }
            catch (Exception ex)
            {
                // log and return unexpected errors
                Console.WriteLine($"Firestore update failed for user {userId}: {ex.Message}");
                return (false, $"Unexpected error updating Firestore: {ex.Message}");
            }
        }

        // method to get paid invoices within date range
        public async Task<List<Invoice>> GetPaidInvoicesByDateRangeAsync(int months)
        {
            try
            {
                // fetch all invoices from firestore
                var invoicesRef = _firestoreDb.Collection("invoices");
                var snapshot = await invoicesRef.GetSnapshotAsync();

                DateTime cutoffDate = DateTime.UtcNow.AddMonths(-months);
                List<Invoice> paidInvoices = new();

                // local safe decryption helper
                string SafeDecrypt(string value)
                {
                    if (string.IsNullOrWhiteSpace(value))
                        return value;

                    try
                    {
                        // attempt decryption
                        return _encryptionHelper.Decrypt(value);
                    }
                    catch
                    {
                        // return the original value if not encrypted
                        return value;
                    }
                }

                foreach (var doc in snapshot.Documents)
                {
                    if (!doc.Exists) continue;
                    var invoice = doc.ConvertTo<Invoice>();

                    // use safe decrypt for every possibly encrypted field
                    invoice.ClientName = SafeDecrypt(invoice.ClientName);
                    invoice.CompanyName = SafeDecrypt(invoice.CompanyName);
                    invoice.InvoiceNumber = SafeDecrypt(invoice.InvoiceNumber);
                    invoice.Email = SafeDecrypt(invoice.Email);
                    invoice.Phone = SafeDecrypt(invoice.Phone);

                    // decrypt item descriptions
                    if (invoice.CreatedAt == null) continue;
                    DateTime createdAt = invoice.CreatedAt.Value.ToDateTime();

                    // filter for paid invoices within date range
                    if (invoice.Status == "Paid" && createdAt >= cutoffDate)
                    {
                        paidInvoices.Add(invoice);
                    }
                }

                // return sorted list by created date descending
                return paidInvoices.OrderByDescending(i => i.CreatedAt).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching paid invoices: {ex.Message}");
                return new List<Invoice>();
            }
        }

        // method to generate payments report pdf
        public async Task<(byte[] PdfBytes, string FileName)> GeneratePaymentsReportPdfAsync(List<Invoice> invoices, int months)
        {
            // validate input
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
            table.ResetCells(invoices.Count + 1, 4);

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

        private CollectionReference GetPaymentsCollection()
        {
            return _firestoreDb.Collection("Payments");
        }

        private async Task AddPaymentRecordAsync(string invoiceId)
        {
            // Get the invoice document
            var invoiceDoc = await GetInvoicesCollection().Document(invoiceId).GetSnapshotAsync();

            if (invoiceDoc.Exists)
            {
                var invoiceData = invoiceDoc.ToDictionary();

                // Create a new Payment record using the invoice data
                var paymentData = new Dictionary<string, object>
                {
                    { "invoiceId", invoiceId },
                    { "clientName", invoiceData.ContainsKey("clientName") ? invoiceData["clientName"] : "" },
                    { "amount", invoiceData.ContainsKey("amount") ? invoiceData["amount"] : 0 },
                    { "dateIssued", invoiceData.ContainsKey("dateIssued") ? invoiceData["dateIssued"] : null },
                    { "status", "Paid" },
                    { "paymentDate", DateTime.UtcNow },
                    { "createdAt", Timestamp.GetCurrentTimestamp() }
                };

                // Add the payment record to the Payments collection
                await GetPaymentsCollection().AddAsync(paymentData);
            }
        }

        public CollectionReference GetNotificationCollection()
        {
            return _firestoreDb.Collection("notifications");
        }
        public async Task<List<Notifications>> GetRecentNotificationsAsync()
        {
            var notificationsRef = GetNotificationCollection();
            var snapshot = await notificationsRef.GetSnapshotAsync();

            var allNotifications = snapshot.Documents
                .Select(doc => doc.ConvertTo<Notifications>())
                .ToList();

            // Filter notifications to only those within the last 7 days
            var oneWeekAgo = DateTime.UtcNow.AddDays(-7);
            var recentNotifications = allNotifications
                .Where(n => n.timestamp.ToDateTime() >= oneWeekAgo)
                .OrderByDescending(n => n.timestamp.ToDateTime())
                .ToList();

            return recentNotifications;
        }

    }
}
//-------------------------------------------------------------------------------------------End Of File--------------------------------------------------------------------//