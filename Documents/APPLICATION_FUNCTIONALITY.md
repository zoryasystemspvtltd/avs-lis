# AVS-LIS (Laboratory Information System) - Application Functionality

## Table of Contents
1. [Project Overview](#project-overview)
2. [Architecture](#architecture)
3. [Core Modules](#core-modules)
4. [Key Features](#key-features)
5. [API Endpoints](#api-endpoints)
6. [Database Models](#database-models)
7. [User Management & Security](#user-management--security)
8. [Integrations](#integrations)
9. [Reporting & Analytics](#reporting--analytics)
10. [Quality Control](#quality-control)
11. [Notifications System](#notifications-system)
12. [Support Components](#support-components)

---

## Project Overview

**AVS-LIS** is a comprehensive **Laboratory Information System (LIS)** designed for managing laboratory operations in healthcare facilities. It integrates with Hospital Information Systems (HIS) and laboratory equipment to streamline sample collection, testing, result management, and reporting workflows.

### Technology Stack
- **Framework:** .NET Framework 4.8
- **API:** ASP.NET Web API
- **Authentication:** OAuth 2.0 with JWT Bearer Tokens
- **Database:** Entity Framework with SQL Server
- **Real-time Communication:** SignalR
- **Logging:** Custom Logger
- **Dependency Injection:** Simple Injector

### Target Users
- Laboratory Administrators
- Lab Technicians
- Doctors/Clinicians
- Patients (via reports)
- Hospital Administrators

---

## Architecture

### Solution Structure

```
avs-lis/
├── web/
│   ├── Lis.Api/                    # Main Web API
│   ├── Lis.Api.Client/             # API Client Library
│   └── Lis.Api.Client.Test/        # Client Tests
├── LIS.BusinessLogic/              # Business Logic Layer
├── LIS.DtoModel/                   # Data Transfer Objects
├── LIS.DataModel/                  # Database Models (EF)
├── LIS.Logger/                     # Logging Service
├── LIS.Com.Businesslogic/          # Common Business Logic
├── LisTCPIPConsole/                # TCP/IP Console App
├── BarcodePrint/                   # Barcode Printing Module
└── Test.COM/                       # Tests
```

### Layered Architecture
1. **API Layer** (Lis.Api) - RESTful endpoints
2. **Business Logic Layer** (LIS.BusinessLogic) - Domain operations
3. **Data Access Layer** (LIS.DataAccess) - Database operations
4. **DTO Layer** (LIS.DtoModel) - Data contracts & interfaces

---

## Core Modules

### 1. **Sample Management**
Handles the complete lifecycle of laboratory samples.

#### Components:
- **SampleController** - Core sample operations
- **SampleCollectionManager** - Manages sample collection workflow
- **SampleReceivingManager** - Handles sample receiving and validation
- **SpecimenManager** - Manages specimen types and properties
- **RawSampleController** - Raw sample data handling

#### Key Functions:
- Sample collection from patients
- Specimen type management
- Sample receiving and validation
- Sample rejection reason tracking
- Sample workflow status tracking
- Barcode generation and management

---

### 2. **Test Management**
Comprehensive test configuration and execution management.

#### Components:
- **LisTestController** - Test configuration endpoints
- **TestParameterManager** - Manages test parameters
- **TestProfileMasterManager** - Handles test profiles
- **TestRateMasterManager** - Test pricing management
- **TestMappingMasterManager** - Test-to-equipment mappings
- **HisTestMasterManager** - HIS integration for tests

#### Key Functions:
- Test definition and configuration
- Test parameter setup
- Test profile creation (grouped tests)
- Test rate/pricing management
- Test-to-equipment mapping for automation
- Integration with HIS test master

---

### 3. **Patient Management**
Manages patient information and hospital interactions.

#### Components:
- **PatientsController** - Patient CRUD operations
- **PatientDetailManager** - Detailed patient management
- **PatientVisitManager** - Patient visit tracking

#### Key Functions:
- Patient registration and profile management
- Patient visit tracking
- Patient demographic management
- Patient-test-result linkage

---

### 4. **Results & Reporting**
Handles test result entry, validation, and report generation.

#### Components:
- **ResultsController** - Result entry endpoints
- **ResultManager** - Result processing logic
- **ReportController** - Report generation
- **ReportDownloadController** - Report download handling
- **ReportManager** - Report template management
- **ReportLayoutConfigurationManager** - Report layout customization
- **ReportTemplateManager** - Report template design & management
- **RadiologyReportController** - Radiology-specific reports
- **RadiologyReportManager** - Radiology report handling

#### Key Functions:
- Test result entry and validation
- Automatic result range evaluation
- Result approval workflow
- Report template design
- Report layout customization
- Multi-format report generation
- Radiology report support
- Report download capabilities

---

### 5. **Equipment Integration**
Integration with laboratory analyzer equipment.

#### Components:
- **EquipmentsController** - Equipment management
- **EquipmentManager** - Equipment lifecycle management
- **EquipmentHeartBeatManager** - Equipment health monitoring
- **EquipmentParamMappingManager** - Parameter-to-equipment mapping
- **EquipmentTestMappingManager** - Test-to-equipment mapping
- **HeartbeatController** - Equipment heartbeat monitoring

#### Key Functions:
- Equipment registration and configuration
- Equipment parameter mapping
- Equipment-test capability mapping
- Equipment status monitoring (heartbeat)
- TCP/IP connection management for equipment
- Equipment availability tracking

---

### 6. **Department & Organization Management**
Controls department structure and configuration.

#### Components:
- **DepartmentController** - Department management
- **DepartmentManager** - Department operations
- **DepartmentProcessingRouter** - Department workflow routing
- **HospitalsController** - Hospital management

#### Key Functions:
- Department creation and management
- Department-wise processing categories
- Workflow routing by department
- Hospital/facility management
- Multi-facility support

---

### 7. **Dashboard & Analytics**
Real-time operational metrics and dashboard.

#### Components:
- **DashboardController** - Dashboard data endpoints
- **DashboardManager** - Dashboard metric calculation
- **OperationalReportsController** - Operational reports

#### Key Features:
- Real-time sample workflow metrics
- Registration and billing metrics
- Doctor approval pending count
- Collection overdue tracking
- Doctor authorization status
- Sample rejection tracking
- Revenue metrics
- Department-wise statistics
- Customizable dashboard widgets

---

### 8. **Quality Control Management**
QC process management for lab accuracy.

#### Components:
- **QualityController** - QC operations
- **QualityControlManager** - QC logic
- **IQualityControlResult** - QC result interface

#### Key Functions:
- Quality control sample management
- Control result tracking
- QC parameter validation
- QC status monitoring

---

### 9. **Billing & Sales**
Invoice and sales management.

#### Components:
- **SaleInvoiceController** - Invoice operations
- **SaleInvoiceManager** - Invoice processing
- **TestRateController** - Test rate management

#### Key Functions:
- Sale invoice generation
- Invoice line item management
- Billing item lookup
- Invoice tracking and reports
- Automatic invoice numbering

---

### 10. **Notification System**
Multi-channel notification delivery.

#### Components:
- **NotificationConfigurationController** - Configuration management
- **NotificationConfigurationManager** - Config logic
- **NotificationManager** - Core notification management
- **NotificationService** - Service interface
- **NotificationEventManager** - Event triggering
- **NotificationProcessor** - Message processing
- **NotificationHttpTransport** - HTTP delivery
- **NotificationTemplateManager** - Template management
- **NotificationSecureDownloadManager** - Secure link distribution
- **NotificationBackgroundDispatcher** - Async processing
- **NotificationProviders** - Provider implementations

#### Key Functions:
- Event-driven notifications
- Email delivery
- HTTP webhook integration
- Template-based messaging
- Notification configuration per user/role
- Audit trail for notifications
- Secure download links
- Background processing

---

### 11. **User Management & Access Control**
Authentication, authorization, and user administration.

#### Components:
- **AccountController** - User account management
- **UsersController** - User CRUD operations
- **RolesController** - Role management
- **UserAccessController** - Role-user mapping
- **UserProfileController** - User profile management
- **ResetPasswordController** - Password reset functionality
- **AuthRepository** - Authentication repository
- **ApplicationOAuthProvider** - OAuth token provider
- **ApplicationRefreshTokenProvider** - Token refresh logic

#### Key Functions:
- User registration and provisioning
- Role-based access control (RBAC)
- Permission management per role
- User-role assignments
- Module-permission mapping
- Password management
- OAuth 2.0 authentication
- JWT Bearer token support
- Token refresh mechanism
- Menu-permission association

---

### 12. **HIS (Hospital Information System) Integration**
Two-way integration with HIS systems.

#### Components:
- **HisParameterController** - HIS parameter sync
- **HisParameterRangeController** - Parameter range management
- **HisTestController** - HIS test master sync
- **HISParameterRangeMasterManager** - Range management logic
- **HISTestMasterManager** - Test master management
- **ExternalApiManager** - External API interactions
- **ExternalApiMaster** - Entity for external integrations

#### Key Functions:
- Synchronize test definitions from HIS
- Import parameter reference ranges
- Export test results to HIS
- Patient visit information reception
- Test request intake from HIS
- Bidirectional data exchange
- External API configuration

---

### 13. **Barcode Management**
Barcode generation and processing.

#### Components:
- **BarCodeController** - Barcode endpoints
- **BarcodePrint** - Barcode printing module
- **BarcodeElements** - Barcode DTO models

#### Key Functions:
- Barcode generation for samples
- Barcode printing
- Barcode scanning integration

---

### 14. **Lookup Masters**
Static reference data management.

#### Components:
- **LookupMasterControllers** - Lookup management
- **LookupMasterManagers** - Lookup logic

#### Supported Lookups:
- Sample types
- Test categories
- Test groups
- Methods
- Units
- Containers
- Corporate/Facility masters
- Referral doctors
- Sample rejection reasons

---

## Key Features

### Workflow Management
- **Sample Collection Workflow** - From patient registration to collection
- **Sample Receiving** - Validation and acceptance of samples
- **Test Execution** - Routing to appropriate equipment
- **Result Entry & Validation** - Multi-step validation process
- **Doctor Authorization** - Approval workflow for results
- **Report Generation** - Multiple format support
- **Result Delivery** - Patient/Doctor notification

### Sample Processing
- Sample collection by date rules
- Sample receiving by date rules
- Specimen rejection handling
- Multi-specimen support per request
- Sample status tracking
- Department-wise processing

### Result Management
- Parameter-wise result entry
- Automatic range validation
- Reference range support
- Critical value handling
- Manual result editing with audit trail
- Result approval workflow
- Result correction capability

### Report Generation
- Declarative report designer
- Multiple template support
- Custom report layouts
- Report versioning
- Role-based report access
- Report scheduling
- PDF export capability

### Financial Management
- Test rate configuration
- Insurance integration
- Invoice generation
- Sales reporting
- Revenue tracking
- Payment tracking

---

## API Endpoints

### Authentication & User Management
```
POST   /api/account/register           # User registration
POST   /api/account/login              # OAuth login
POST   /api/account/token              # Token generation
POST   /api/users                      # Create user
GET    /api/users                      # Get all users
GET    /api/users/{id}                 # Get user details
PUT    /api/users/{id}                 # Update user
DELETE /api/users/{id}                 # Delete user
POST   /api/roles                      # Create role
GET    /api/roles                      # Get roles
PUT    /api/roles/{id}                 # Update role
POST   /api/useraccess                 # Assign role to user
GET    /api/userprofile                # Get user profile
POST   /api/resetpassword              # Reset password
```

### Patient Management
```
POST   /api/patients                   # Register patient
GET    /api/patients                   # Get patients
GET    /api/patients/{id}              # Get patient details
PUT    /api/patients/{id}              # Update patient
DELETE /api/patients/{id}              # Delete patient
```

### Sample Management
```
POST   /api/sample                     # Create test request & samples
GET    /api/sample                     # Get samples
GET    /api/sampledetails              # Get sample details
PUT    /api/sample/{id}                # Update sample
GET    /api/samplecollection           # Get collection list
POST   /api/samplecollection           # Mark collected
GET    /api/samplereceiving            # Get receiving list
POST   /api/samplereceiving            # Receive samples
POST   /api/samplecollectionstatus     # Update collection status
POST   /api/newsample                  # Create new sample
```

### Test Management
```
GET    /api/listest                    # Get tests
POST   /api/listest                    # Create test
PUT    /api/listest/{id}               # Update test
DELETE /api/listest/{id}               # Delete test
GET    /api/testparameter              # Get parameters
POST   /api/testparameter              # Create parameter
GET    /api/testprofilemaster          # Get profiles
POST   /api/testprofilemaster          # Create profile
GET    /api/testratemaster             # Get rates
POST   /api/testratemaster             # Create rate
```

### Result Management
```
POST   /api/results                    # Enter results
GET    /api/results                    # Get results
PUT    /api/results/{id}               # Update results
POST   /api/results/Authorize          # Authorize results
POST   /api/results/Reject             # Reject results
POST   /api/testrresultedit            # Edit results with audit
POST   /api/rawparameters              # Get raw parameters
```

### Equipment Management
```
GET    /api/equipments                 # Get equipment list
POST   /api/equipments                 # Create equipment
PUT    /api/equipments/{id}            # Update equipment
DELETE /api/equipments/{id}            # Delete equipment
POST   /api/equipmentparam             # Map parameters
POST   /api/equipmenttest              # Map tests
GET    /api/heartbeat/{equipmentId}    # Get equipment status
```

### Reporting
```
GET    /api/report                     # Get reports
POST   /api/report                     # Generate report
GET    /api/report/{id}                # Get report details
POST   /api/reportdownload             # Download report
POST   /api/radiology                  # Radiology report operations
GET    /api/reportlayout               # Get layout configs
POST   /api/reporttemplate             # Manage templates
```

### Dashboard & Analytics
```
GET    /api/dashboard/registration     # Registration metrics
GET    /api/dashboard/processing       # Processing metrics
GET    /api/dashboard/approval         # Approval metrics
GET    /api/operationalreports         # Operational reports
```

### Billing
```
POST   /api/saleinvoice                # Create invoice
GET    /api/saleinvoice                # Get invoices
GET    /api/saleinvoice/{id}           # Get invoice details
PUT    /api/saleinvoice/{id}           # Update invoice
```

### Notifications
```
GET    /api/notificationconfig         # Get configurations
POST   /api/notificationconfig         # Create configuration
PUT    /api/notificationconfig/{id}    # Update configuration
DELETE /api/notificationconfig/{id}    # Delete configuration
```

### HIS Integration
```
GET    /api/histest                    # Get HIS tests
POST   /api/histest                    # Sync HIS tests
GET    /api/hisparameter               # Get HIS parameters
POST   /api/hisparameter               # Sync parameters
GET    /api/hisparameter/range         # Get parameter ranges
```

### Masters
```
GET    /api/lookupmaster               # Get lookup values
POST   /api/lookupmaster               # Create lookup
GET    /api/departments                # Get departments
POST   /api/departments                # Create department
GET    /api/hospitals                  # Get hospitals
POST   /api/hospitals                  # Create hospital
```

---

## Database Models

### Core Entities

#### Patient Information
- **PatientDetail** - Patient demographics and identification
- **PatientVisit** - Patient visit records

#### Sample Management
- **TestRequestDetail** - Test requisition from patient
- **SampleItem** - Physical sample information
- **Specimen** - Specimen type classification

#### Test Definitions
- **LisTest** - Laboratory test definition
- **TestParameter** - Parameter within a test
- **TestProfileMaster** - Grouped tests (profiles)
- **TestParameterRange** - Reference ranges

#### Results
- **TestResult** - Test result entry
- **TestResultDetails** - Parameter-level results
- **ControlResult** - QC control results

#### Equipment
- **EquipmentMaster** - Laboratory equipment
- **EquipmentHeartBeat** - Equipment status monitoring
- **EquipmentTestMapping** - Equipment capabilities
- **EquipmentParamMapping** - Equipment-parameter mappings

#### Financial
- **SaleInvoice** - Invoice header
- **SaleInvoiceDetails** - Invoice line items
- **TestRateMaster** - Test rate configuration

#### Reporting
- **ReportTemplate** - Report design templates
- **ReportLayoutConfiguration** - Layout customization
- **RadiologyRequestDetail** - Radiology requests
- **RadiologyResultDetail** - Radiology results

#### Organization
- **Department** - Lab department structure
- **Hospital/Corporate** - Facility information

#### User & Security
- **AspNetUser** - User accounts (ASP.NET Identity)
- **AspNetRole** - Role definitions
- **AspNetUserRole** - User-role mappings
- **RoleMenuPermission** - Role-menu-permission mappings
- **RoleModuleMappings** - Role-module access control

#### System
- **NotificationConfiguration** - Notification settings
- **NotificationTemplate** - Message templates
- **LookupMaster** - Reference data

---

## User Management & Security

### Authentication Flow
1. User submits credentials via `/api/account/login`
2. `ApplicationOAuthProvider` validates credentials
3. JWT Bearer token generated with claims
4. Token expires after configurable period
5. `ApplicationRefreshTokenProvider` allows token refresh
6. Subsequent requests include token in Authorization header

### Authorization
- **Module-Level**: `ModuleIdentity` tracks user's accessible modules
- **Feature-Level**: `QAuthorize` attribute checks specific permissions
- **Role-Based**: `AspNetRoles` define access levels
- **Menu-Permission**: `RoleMenuPermission` maps roles to menu items
- **Resource-Level**: LINQ queries filter data by user/organization

### Permission Types
- `CanCreate` - Create new records
- `CanRead` - View records
- `CanUpdate` - Edit existing records
- `CanDelete` - Remove records
- `CanAuthorize` - Approve results
- `CanReject` - Reject operations
- `CanReport` - Access reports
- `CanManageMaster` - Configure masters
- `CanApprove` - Approve various operations

### Password Security
- Password reset via `ResetPasswordController`
- Secure token generation
- Password history tracking
- Configurable expiration policies

---

## Integrations

### HIS (Hospital Information System) Integration
- **Import**: Test requests, patient visits, test definitions, parameter ranges
- **Export**: Test results, report data, sample status updates
- **Protocol**: HTTP API calls via `ExternalApiManager`
- **Synchronization**: Real-time or batch processing

### Equipment Integration
- **TCP/IP Protocol**: Direct connection via `LisTCPIPConsole`
- **Data Exchange**: Command execution, result retrieval
- **Heartbeat Monitoring**: Continuous equipment health checks
- **Automatic Routing**: Results from equipment to LIS

### Notification Integration
- **Email**: SMTP-based delivery
- **HTTP Webhook**: External system notifications
- **SMS**: Configurable provider
- **Custom**: Extensible notification providers

### Barcode Integration
- **Generation**: Via `BarcodePrint` module
- **Scanning**: Sample identification
- **Printing**: Barcode label generation

---

## Reporting & Analytics

### Report Types

#### Diagnostic Test Reports
- Test-wise parametric results
- Multiple page support
- Certified/Approved marking
- Referral doctor indication
- Rate and amount display

#### Radiology Reports
- Imaging findings
- Impression summary
- Doctor authorization
- Report versioning

#### Operational Reports
- Sample workflow statistics
- Test execution metrics
- Equipment utilization
- Department performance
- Doctor authorization metrics

#### Financial Reports
- Sales invoice register
- Test booking register
- Revenue tracking
- Payment collection

#### Quality Reports
- QC result tracking
- Out-of-control notifications
- Trend analysis

### Report Design Features
- **Declarative Designer**: Define report structure
- **Template Support**: Reusable report templates
- **Layout Customization**: Adjust appearance per requirement
- **Dynamic Fields**: Auto-populate from data
- **Conditional Sections**: Show/hide based on data
- **Multi-page**: Support for long reports
- **Export Formats**: PDF, Excel, HTML

### Report Distribution
- Direct download from LIS
- Secure email delivery
- Secure download links (time-limited)
- Patient portal access

---

## Quality Control

### QC Management Features
- Control sample definition
- Control result entry
- Multi-level QC (inter/intra-assay)
- Reference range validation
- Trend monitoring
- Out-of-control alerts
- QC certificate generation

### Quality Metrics
- Precision (CV %)
- Accuracy (bias)
- Linearity
- Carry-over detection

---

## Notifications System

### Event Types
- Sample received
- Results ready
- Doctor authorization required
- Test overdue
- Equipment offline
- QC failure
- Invoice generated
- Payment received

### Channels
- **Email**: SMTP
- **HTTP Webhook**: External systems
- **SMS**: Configurable providers
- **In-App**: Browser notifications via SignalR

### Features
- Template-based messaging
- User preference management
- Event filtering by role/department
- Retry mechanism
- Delivery audit trail
- Secure document distribution

### Configuration
- Per-user notification settings
- Channel preferences
- Event subscription management
- Frequency control (real-time, digest, daily)

---

## Support Components

### Logging
- **LIS.Logger**: Custom logging framework
- Structured logging with JSON
- Log levels: Debug, Info, Warning, Error, Critical
- File and database logging support

### Helper Utilities
- **Config**: Configuration management
- **ApiClient**: HTTP client for external APIs
- **OperationalDateTime**: Time zone handling
- **SampleCollectionDateRules**: Collection timing logic
- **SampleReceivingDateRules**: Receiving timing logic
- **ObjectExtensionMethods**: Common LINQ extensions

### Dependency Injection
- **SimpleInjectorConfig**: Container setup
- Automatic dependency resolution
- Lifecycle management (singleton/transient/scoped)

### Real-time Communication
- **SignalR**: WebSocket support
- **LisHub**: Hub for push notifications
- Event broadcasting to connected clients

### Data Transfer
- **Dto Models**: Type-safe contracts
- **Serialization**: JSON formatting
- **Mapping**: Entity to DTO conversion

### Error Handling
- **ApiResponse**: Standard response envelope
- **ApiError**: Error detail encapsulation
- **ValidationError**: Field-level validation
- HTTP status codes mapping

---

## Configuration & Operations

### Application Settings
- Connection strings (configured in Web.config)
- OAuth token expiry
- Database entity paths
- External API endpoints
- Email server settings
- Notification delivery settings
- Report storage paths

### Database Migrations
```powershell
# LIS API Database
Add-Migration -ProjectName Lis.Api -StartUpProjectName Lis.Api
Update-Database -configuration Lis.Api.Migrations.Configuration -Verbose -ProjectName Lis.Api -StartUpProjectName Lis.Api

# LIS Data Model
Add-Migration -ProjectName LIS.DataAccess -StartUpProjectName Lis.Api
Update-Database -configuration LIS.DataAccess.Migrations.Configuration -Verbose -ProjectName LIS.DataAccess -StartUpProjectName Lis.Api
```

### Deployment
- Publish profile: FolderProfile.pubxml
- Web.config transformations for environments
- Entity Framework code-first migrations
- Database initialization scripts

---

## Extensibility Points

### Custom Managers
Implement `IModuleManager` to create new managers:
- Business logic encapsulation
- Data access abstraction
- Validation and error handling

### Custom Controllers
Extend `MasterApiControllerBase` for consistent API patterns:
- Standard CRUD operations
- Security enforcement
- Response formatting

### Custom Reports
Design reports using declarative template engine:
- Field mapping
- Calculation logic
- Conditional display

### Custom Notifications
Implement `INotificationProvider` for additional channels:
- SMS, Push, Custom messaging
- Integration with external systems

### Custom Validations
Extend validation in managers:
- Business rule enforcement
- Cross-entity validation
- Date/time rule evaluation

---

## Summary

**AVS-LIS** is a comprehensive, enterprise-grade Laboratory Information System with:
- Complete sample-to-report workflow automation
- Multi-channel equipment integration
- Advanced reporting and analytics
- Secure user management with fine-grained permissions
- Real-time notifications and monitoring
- Financial management and billing
- Quality control management
- HIS integration capabilities
- Extensible architecture for customization

The system is designed for modern laboratories requiring reliable, scalable, and secure operations management with seamless HIS integration.
