using System.Collections.Generic;
using System.Linq;

namespace Lis.Api.Providers
{
    /// <summary>
    /// Static catalog of navigable menus. Additive overlay on UserModules — does not replace modules.
    /// </summary>
    public static class MenuCatalog
    {
        public class MenuDefinition
        {
            public string MenuKey { get; set; }
            public string ModuleName { get; set; }
            public string Section { get; set; }
            public string Label { get; set; }
            public string Route { get; set; }
            public int Order { get; set; }
        }

        private static readonly List<MenuDefinition> Items = new List<MenuDefinition>
        {
            // Working Board
            M("WORKING_BOARD_SAMPLES", "Samples", "Working Board", "Recent Samples", "/samples", 10),
            M("WORKING_BOARD_COLLECTION", "SampleCollection", "Working Board", "Sample Collection", "/sample-collection", 20),
            M("WORKING_BOARD_RECEIVING", "SampleReceiving", "Working Board", "Sample Receiving", "/sample-receiving", 30),
            M("WORKING_BOARD_RADIOLOGY_ENTRY", "RadiologyReportEntry", "Working Board", "Radiology Entry", "/radiology-report-entry", 40),
            M("WORKING_BOARD_RADIOLOGY_APPROVAL", "RadiologyDoctorApprovals", "Working Board", "Radiology Doctor Approval", "/radiology-doctor-approvals", 50),
            M("WORKING_BOARD_RADIOLOGY_APPROVED", "RadiologyDoctorApprovals", "Working Board", "Approved Radiology Reports", "/radiology-approved-reports", 60),
            M("WORKING_BOARD_TECHNICIAN_APPROVAL", "Reports", "Working Board", "Technician Approval", "/technicianapprovals", 70),
            M("WORKING_BOARD_LAB_RESULT_EDIT", "Reports", "Working Board", "Lab Result Entry", "/edit-test-results", 80),
            M("WORKING_BOARD_DOCTOR_APPROVAL", "DoctorsApprovals", "Working Board", "Doctor Approval", "/doctorapprovals", 90),
            M("WORKING_BOARD_APPROVED_SAMPLES", "Reports", "Working Board", "Approved Samples", "/approvedsamples", 100),
            M("WORKING_BOARD_REJECTED_SAMPLES", "Reports", "Working Board", "Rejected Samples", "/rejectedsamples", 110),
            M("WORKING_BOARD_QUALITY_CONTROLS", "Reports", "Working Board", "Quality Controls", "/quality-controls", 120),

            // Setup (Masters + Equipments)
            M("MASTER_DEPARTMENT", "Masters", "Masters", "Department", "/departments", 10),
            M("MASTER_UNIT", "Masters", "Masters", "Unit", "/units", 20),
            M("MASTER_METHOD", "Masters", "Masters", "Method", "/methods", 30),
            M("SETUP_EQUIPMENT", "Equipments", "Masters", "Equipment", "/equipments", 40),
            M("SETUP_EQUIPMENT_HEARTBEAT", "Equipments", "Masters", "Equipment Heartbeat", "/equipment-heartbeat", 50),
            M("SETUP_NOTIFICATION_CONFIGURATION", "NotificationConfiguration", "Masters", "Notification Configuration", "/notification-configuration", 55),
            M("SETUP_REPORT_LAYOUT_CONFIGURATION", "ReportLayoutConfiguration", "Masters", "Report Layout Configuration", "/report-layout-configuration", 56),
            M("SETUP_REPORT_TEMPLATE_CONFIGURATION", "ReportTemplateConfiguration", "Masters", "Report Template Configuration", "/report-template-configuration", 57),

            // Master data
            M("MASTER_TESTMASTER", "HisTest", "Masters", "Test Master", "/test-master", 60),
            M("MASTER_TESTPROFILE", "Masters", "Masters", "Test Profile", "/test-profiles", 70),
            M("MASTER_SPECIMEN", "Masters", "Masters", "Specimen", "/specimens", 80),
            M("MASTER_TESTRATE", "TestRates", "Masters", "Test Rate", "/test-rates", 90),
            M("MASTER_REFERRAL_DOCTOR", "Masters", "Masters", "Referral Doctor", "/referral-doctors", 100),
            M("MASTER_CORPORATE", "Masters", "Masters", "Corporate", "/corporates", 110),
            M("MASTER_PARAMETER", "Masters", "Masters", "Parameter Master", "/his-parameters", 120),
            M("MASTER_TEST_PARAM_MAPPING", "Masters", "Masters", "Test Parameter Mapping", "/test-parameters", 130),
            M("MASTER_ANALYZER_PARAM_MAPPING", "Masters", "Masters", "Analyzer Parameter Mapping", "/test-mappings", 140),
            M("MASTER_PARAMETER_RANGE", "Masters", "Masters", "Parameter Range", "/his-parameter-ranges", 150),

            // Transaction
            M("TRANSACTION_PATIENT", "PatientDetails", "Transaction", "Patient Details", "/patient-master", 10),
            M("TRANSACTION_SALEINVOICE", "SaleInvoices", "Transaction", "Sale Invoice", "/sale-invoices", 20),

            // Reports
            M("REPORT_INVOICE_REGISTER", "Reports", "Reports", "Invoice Reports", "/reports/sale-invoice-register", 10),
            M("REPORT_TEST_BOOKING", "Reports", "Reports", "Test Booking Register", "/reports/test-booking-register", 20),
            M("REPORT_DIAGNOSTIC", "Reports", "Reports", "Diagnostic Report", "/reports/test-report", 30),
            M("REPORT_RADIOLOGY", "RadiologyReports", "Reports", "Radiology Reports", "/reports/radiology-report", 40),
            M("REPORT_COLLECTION_SUMMARY", "Reports", "Reports", "Collection Reports", "/reports/collection-summary", 50),
            M("REPORT_COLLECTOR_WISE", "Reports", "Reports", "Collector Wise Report", "/reports/collector-wise", 60),
            M("REPORT_PENDING_COLLECTION", "Reports", "Reports", "Pending Collection", "/reports/pending-collection", 70),
            M("REPORT_RECOLLECTION", "Reports", "Reports", "Recollection Report", "/reports/recollection", 80),
            M("REPORT_RECEIVED_SAMPLES", "Reports", "Reports", "Received Samples", "/reports/received-samples", 90),
            M("REPORT_REJECTED_SAMPLES", "Reports", "Reports", "Rejected Samples", "/reports/rejected-samples", 100),
            M("REPORT_TAT", "Reports", "Reports", "TAT Reports", "/reports/sample-turnaround", 110),
            M("REPORT_RADIOLOGY_PENDING", "RadiologyReports", "Reports", "Pending Radiology Cases", "/reports/radiology/pending", 120),
            M("REPORT_RADIOLOGY_AUTHORIZED", "RadiologyReports", "Reports", "Authorized Radiology", "/reports/radiology/authorized", 130),
            M("REPORT_RADIOLOGY_MODALITY", "RadiologyReports", "Reports", "Modality Statistics", "/reports/radiology/modality-stats", 140),
            M("REPORT_RADIOLOGY_PRODUCTIVITY", "RadiologyReports", "Reports", "Radiologist Productivity", "/reports/radiology/productivity", 150),

            // Account
            M("ACCOUNT_USERS", "Users", "Account", "Users", "/users", 10),
            M("ACCOUNT_ROLES", "Roles", "Account", "Roles", "/roles", 20),
        };

        /// <summary>Legacy keys → current keys (seeded rows / older clients).</summary>
        private static readonly Dictionary<string, string> LegacyAliases = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "workingboard.recentSamples", "WORKING_BOARD_SAMPLES" },
            { "workingboard.sampleCollection", "WORKING_BOARD_COLLECTION" },
            { "workingboard.sampleReceiving", "WORKING_BOARD_RECEIVING" },
            { "workingboard.radiologyReportEntry", "WORKING_BOARD_RADIOLOGY_ENTRY" },
            { "workingboard.radiologyDoctorApproval", "WORKING_BOARD_RADIOLOGY_APPROVAL" },
            { "workingboard.radiologyApproved", "WORKING_BOARD_RADIOLOGY_APPROVED" },
            { "workingboard.technicianApproval", "WORKING_BOARD_TECHNICIAN_APPROVAL" },
            { "workingboard.testResultEdit", "WORKING_BOARD_LAB_RESULT_EDIT" },
            { "workingboard.doctorApproval", "WORKING_BOARD_DOCTOR_APPROVAL" },
            { "workingboard.approvedSamples", "WORKING_BOARD_APPROVED_SAMPLES" },
            { "workingboard.rejectedSamples", "WORKING_BOARD_REJECTED_SAMPLES" },
            { "workingboard.qualityControls", "WORKING_BOARD_QUALITY_CONTROLS" },
            { "setup.department", "MASTER_DEPARTMENT" },
            { "setup.unit", "MASTER_UNIT" },
            { "setup.method", "MASTER_METHOD" },
            { "setup.equipment", "SETUP_EQUIPMENT" },
            { "setup.equipmentHeartbeat", "SETUP_EQUIPMENT_HEARTBEAT" },
            { "setup.notificationConfiguration", "SETUP_NOTIFICATION_CONFIGURATION" },
            { "setup.reportLayoutConfiguration", "SETUP_REPORT_LAYOUT_CONFIGURATION" },
            { "setup.reportTemplateConfiguration", "SETUP_REPORT_TEMPLATE_CONFIGURATION" },
            { "masters.testMaster", "MASTER_TESTMASTER" },
            { "masters.testProfile", "MASTER_TESTPROFILE" },
            { "masters.specimen", "MASTER_SPECIMEN" },
            { "masters.testRate", "MASTER_TESTRATE" },
            { "masters.referralDoctor", "MASTER_REFERRAL_DOCTOR" },
            { "masters.corporate", "MASTER_CORPORATE" },
            { "masters.parameter", "MASTER_PARAMETER" },
            { "masters.testParamMapping", "MASTER_TEST_PARAM_MAPPING" },
            { "masters.analyzerParamMapping", "MASTER_ANALYZER_PARAM_MAPPING" },
            { "masters.parameterRange", "MASTER_PARAMETER_RANGE" },
            { "transaction.patientDetails", "TRANSACTION_PATIENT" },
            { "transaction.saleInvoice", "TRANSACTION_SALEINVOICE" },
            { "reports.saleInvoiceRegister", "REPORT_INVOICE_REGISTER" },
            { "reports.testBookingRegister", "REPORT_TEST_BOOKING" },
            { "reports.diagnosticReport", "REPORT_DIAGNOSTIC" },
            { "reports.radiologyReportPrint", "REPORT_RADIOLOGY" },
            { "reports.collectionSummary", "REPORT_COLLECTION_SUMMARY" },
            { "reports.collectorWise", "REPORT_COLLECTOR_WISE" },
            { "reports.pendingCollection", "REPORT_PENDING_COLLECTION" },
            { "reports.recollection", "REPORT_RECOLLECTION" },
            { "reports.receivedSamples", "REPORT_RECEIVED_SAMPLES" },
            { "reports.rejectedSamples", "REPORT_REJECTED_SAMPLES" },
            { "reports.turnaround", "REPORT_TAT" },
            { "reports.radiologyPending", "REPORT_RADIOLOGY_PENDING" },
            { "reports.radiologyAuthorized", "REPORT_RADIOLOGY_AUTHORIZED" },
            { "reports.radiologyModality", "REPORT_RADIOLOGY_MODALITY" },
            { "reports.radiologyProductivity", "REPORT_RADIOLOGY_PRODUCTIVITY" },
            { "account.users", "ACCOUNT_USERS" },
            { "account.roles", "ACCOUNT_ROLES" },
        };

        private static MenuDefinition M(string key, string module, string section, string label, string route, int order)
        {
            return new MenuDefinition
            {
                MenuKey = key,
                ModuleName = module,
                Section = section,
                Label = label,
                Route = route,
                Order = order
            };
        }

        public static IReadOnlyList<MenuDefinition> All
        {
            get { return Items; }
        }

        public static string NormalizeKey(string menuKey)
        {
            if (string.IsNullOrWhiteSpace(menuKey))
            {
                return menuKey;
            }
            string mapped;
            if (LegacyAliases.TryGetValue(menuKey.Trim(), out mapped))
            {
                return mapped;
            }
            return menuKey.Trim();
        }

        public static IEnumerable<MenuDefinition> ForModule(string moduleName)
        {
            return Items.Where(m => m.ModuleName.Equals(moduleName, System.StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m.Order);
        }

        public static IEnumerable<MenuDefinition> ForSection(string section)
        {
            return Items.Where(m => m.Section.Equals(section, System.StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m.Order);
        }

        public static MenuDefinition Find(string menuKey)
        {
            var key = NormalizeKey(menuKey);
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }
            return Items.FirstOrDefault(m => m.MenuKey.Equals(key, System.StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsKnown(string menuKey)
        {
            return Find(menuKey) != null;
        }
    }
}
