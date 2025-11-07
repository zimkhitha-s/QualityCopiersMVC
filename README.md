<img width="1004" height="404" alt="image" src="https://github.com/user-attachments/assets/38eaee96-a1a1-4d37-b878-ada245e83f83" />


# Elevate digital studios presents : 
## Quality Copiers Management Web application.

A full-stack web-based management system developed for **Quality Copiers**, a small business based in Cape Town, South Africa.  
This project was built as part of an **INSY7315** group project at Varsity College, integrating business and technology to streamline management operations for the company.

<br>
<img width="6000" height="3375" alt="INSY7315 - ElevateDigitalStudios -Presentation (1)" src="https://github.com/user-attachments/assets/94ea5a98-2df1-4961-9ccc-6868e4298572" />

## Overview

The **Quality Copiers Management System (MVC)** has two major sections: 

1. The front facing website:
- Acts as the company’s **official website**.
- Allows visitors and potential clients to:
   - Learn about the business
   - Request copier repair or maintenance services
   - Submit contact forms
   - View company information, address, and service details
   
3. Management Dashboard:
A secure dashboard for company operations - an exact replica of the android application. 
- This dashboard will be used by:
  - **Managers (Craig)** → full access to all data  
  - **Employees** → limited access to assigned areas
- The web application provides functionality for:
  - Managing clients and employees
  - Viewing reports
  - Tracking service requests
  - Managing user data (CRUD operations)
  - Role-based UI rendering
   
## User Roles

The system supports two main roles with distinct access levels:
| Role | Description | Access Level |
|------|--------------|--------------|
| **Manager (Craig)** | Business owner, full access to all database collections | Full CRUD access |
| **Employee** | Staff user linked to a manager account | Limited access (based on assigned permissions) |

### Role-Based UI Switching
When a user signs in, the app determines their role from Firestore and automatically adjusts:
- Navigation bar options
- Visible features
- Database permissions
  
This ensures that managers can view and control all records, while employees only interact with relevant data.

## Link to Presentation:
https://www.canva.com/design/DAG2pMwQmqM/9ZLudWY3RdFXDQkJ6v1SAQ/edit?utm_content=DAG2pMwQmqM&utm_campaign=designshare&utm_medium=link2&utm_source=sharebutton 

## Craigs Original Website VS the One we Created

<img width="6000" height="3375" alt="INSY7315 - ElevateDigitalStudios -Presentation (9)" src="https://github.com/user-attachments/assets/a0b19808-4c81-4c97-9e9b-85f53db751a5" />

<br>

<img width="6000" height="3375" alt="INSY7315 - ElevateDigitalStudios -Presentation (10)" src="https://github.com/user-attachments/assets/05fb3765-ff85-4e1c-b737-1987ae908fc0" />


## Security
- Authentication managed through Firebase Authentication (Email/Password)
- All data stored in Firebase Firestore 
- Sensitive fields (like stored documents and internal IDs) are protected through Firestore’s security rules  
- Email and UID remain readable for identification and linkage between users  

## Firebase Database Structure 
```
users/
│
├── {manager_uid}/
│   ├── manager_data/
│   │   └── {manager_uid}
│   ├── employees/
│   │   └── {employee_uid}
│   └── clients/
│       └── {client_id}
│
└── {employee_uid}/
    └── linked_manager: {manager_uid}
```
## Technologies Used

- ASP.NET MVC
- Firebase Authentication
- Firebase Firestore Database
- Entity Framework
- HTML5, CSS3, Bootstrap
- JavaScript/jQuery

## Project Structure 

```
QualityCopiersMVC/
│
├── Controllers/
│   ├── AccountController.cs
│   ├── DashboardController.cs
│   ├── EmployeeController.cs
│   ├── HomeController.cs
│   ├── InvoicesController.cs
│   ├── NotificationsController.cs
│   ├── PaymentsController.cs
│   ├── QuotationsController.cs
│   └── ClientController.cs
│
├── Models/
    |
│   ├── Client.cs
│   ├── Employee.cs
│   ├── ErrorViewModel.cs
|   ├── InvoiceItem.cs
|   ├── Quotation.cs
|   ├── QuotationItem.cs
│   └── Invoice.cs
│
├── Views/
│   ├── Shared/
│   ├── Account/
│   ├── Home/
│   ├── Invoices/
│   ├── Dashboard/
│   ├── Notifactions/
│   ├── Payments/
    ├── Qoutations/
│   ├── Employee/
│   └── Clients/
│
├── wwwroot/
│   ├── css/
│   ├── js/
│   ├── fonts/
│   └── images/
│
├── App.config
├── Global.asax
└── QualityCopiersMVC.csproj
```
## How to Run Locally 

**Prerequisities**
- Visual Studio 2022 or later
- .Net Framework 4.8+
- Firebase project credentials (`google-services.json` or service account key)
- Internet connection

**Steps**
- 1. Clone this Repository
<pre>git clone https://github.com/yourusername/QualityCopiersMVC.git
cd QualityCopiersMVC
</pre>
- 2. Open the Project
- 3. Restore Dependencies
- 4. Add Firebase Configuration
- 5. Run the Project.

**Demo Login Credentials**

| Role         | Email                                                                   | Password    |
| ------------ | ----------------------------------------------------------------------- | ----------- |
| **Manager**  | [craig.diedericks@gmail.com](craigdiedericks@gmail.com)         | Craig12345!  |

<br>

| Role         | Email                                                                   | Password    |
| ------------ | ----------------------------------------------------------------------- | ----------- |
| **Employee**  | [zimkhithasasanti@gmail.com](zimkhithasasanti@@gmail.com)         | Zimkhitha!23  |

## Design vs Actual MVC Visual Studio Code
<img width="6000" height="3375" alt="INSY7315 - ElevateDigitalStudios -Presentation (6)" src="https://github.com/user-attachments/assets/05589c37-4432-45c8-ae3d-0f079fac0764" />
<br>
<img width="6000" height="3375" alt="INSY7315 - ElevateDigitalStudios -Presentation (7)" src="https://github.com/user-attachments/assets/d398e90c-b31f-47b6-8fb5-210b170596d9" />
<br>
<img width="6000" height="3375" alt="INSY7315 - ElevateDigitalStudios -Presentation (3)" src="https://github.com/user-attachments/assets/ee5fe0f5-ddf6-4a40-b8c2-fbbeee5ad3b5" />
<br>
<img width="6000" height="3375" alt="INSY7315 - ElevateDigitalStudios -Presentation (2)" src="https://github.com/user-attachments/assets/2d6362f8-f93a-4bf7-8b9e-3556cfc9349a" />

## Contributors 
- Khanyi Mabuza - Project Manager
- Aman Adams - Bussiness Analyst 
- Thando Fredericks - Lead Backend Developer (MVC) & System Architect
- Thania Mathews - Frontend Developer (Android and MVC) /Backend developer (Android)
- Zimkhitha Sasanti - Lead backend Developer (Android)
- Keenia Geemooi - UI/UX Designer.

<br>
<img width="6000" height="3375" alt="INSY7315 - ElevateDigitalStudios -Presentation (8)" src="https://github.com/user-attachments/assets/279f14e0-2c0a-4f31-bb93-f96b4bf01954" />


  
