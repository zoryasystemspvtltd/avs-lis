import { Routes, RouterModule } from '@angular/router';
import { AuthGuard, PermissionGuard } from './_guards';

const ROUTE_GUARDS = [AuthGuard, PermissionGuard];

import { HomeComponent } from './home/home.component';
import { AboutComponent, ContactComponent, TremsComponent } from './annonimious';
import { LoginComponent, ForgotPasswordComponent, RegisterComponent, ChangePasswordComponent } from './authentication';
import { ProfileComponent } from './authentication/profile/profile.component';
import { ApplicationCreateComponent, ApplicationEditComponent, ApplicationDetailsComponent, ApplicationListComponent } from './administration/applications';
import { UsersListComponent, UsersCreateComponent, UsersEditComponent, UsersDetailsComponent } from './administration/users';
import { RolesListComponent, RolesCreateComponent, RolesEditComponent, RolesDetailsComponent } from './administration/roles';
import { MediaFileListComponent } from './_components';
import { CreateEquipmentComponent, EditEquipmentComponent, ListEquipmentComponent, DetailsEquipmentComponent, ListRawSampleComponent, ListApprovedSampleComponent, ListDoctorSampleComponent, ListRejectedSampleComponent, ListTestedSampleComponent, TechnicianSampleDetailsComponent, RawSampleDetailsComponent, TechnicianSampleSearchComponent, ListQualitySampleComponent, QualityDetailsComponent, EditTestResultsComponent } from './LIS';
import { DetailsParameterComponent } from './LIS/EquipmentParamMapping/details-parameter/details-parameter.component';
import { DoctorSampleDetailsComponent } from './LIS/samples/doctor-details/sample-details.component';
import { HelpComponent } from './annonimious/help/help.component';
import { ApprovedSampleDetailsComponent } from './LIS/samples/approved-sample/sample-details.component';
import { RejectedSampleDetailsComponent } from './LIS/samples/rejected-sample/sample-details.component';
import { CreateSampleComponent } from './LIS/samples/create-sample/create-sample.component';
import { EditSampleComponent } from './LIS/samples/edit-sample/edit-sample.component';
import { TestListComponent } from './LIS/testMaster/test-list/test-list.component';
import { TestDetailsComponent } from './LIS/testMaster/test-details/test-details.component';
import { TestCreateComponent } from './LIS/testMaster/test-create/test-create.component';
import { TestEditComponent } from './LIS/testMaster/test-edit/test-edit.component';
import { MasterListComponent, MasterFormComponent, SaleInvoiceFormComponent, TestProfileFormComponent, TestProfileViewComponent } from './masters';
import { SaleInvoiceRegisterComponent, TestBookingRegisterComponent, TestReportComponent, FddReportComponent, RadiologyReportPrintComponent } from './reports';
import { ListEquipmentHeartbeatComponent } from './LIS';
import { SampleCollectionComponent } from './LIS/sample-workflow/sample-collection/sample-collection.component';
import { SampleReceivingComponent } from './LIS/sample-workflow/sample-receiving/sample-receiving.component';
import { RadiologyReportEntryComponent } from './LIS/radiology/radiology-report-entry/radiology-report-entry.component';
import { RadiologyDoctorApprovalComponent } from './LIS/radiology/radiology-doctor-approval/radiology-doctor-approval.component';
import { RadiologyApprovedReportsComponent } from './LIS/radiology/radiology-approved-reports/radiology-approved-reports.component';

const LOOKUP_FIELDS = {
  codeName: [
    { name: 'code', label: 'Code', type: 'text', required: true },
    { name: 'name', label: 'Name', type: 'text', required: true },
    { name: 'isActive', label: 'Active', type: 'checkbox' }
  ],
  method: [
    { name: 'code', label: 'Code', type: 'text', required: true },
    { name: 'name', label: 'Method Name', type: 'text', required: true, maxLength: 300 },
    { name: 'isActive', label: 'Active', type: 'checkbox' }
  ],
  referral: [
    { name: 'code', label: 'Code', type: 'text', required: true },
    { name: 'name', label: 'Name', type: 'text', required: true },
    { name: 'phone', label: 'Phone', type: 'text' },
    { name: 'email', label: 'Email', type: 'text' },
    { name: 'isActive', label: 'Active', type: 'checkbox' }
  ],
  corporate: [
    { name: 'code', label: 'Code', type: 'text', required: true },
    { name: 'name', label: 'Name', type: 'text', required: true },
    { name: 'defaultDiscountPercent', label: 'Default Discount %', type: 'number' },
    { name: 'isActive', label: 'Active', type: 'checkbox' }
  ],
  testGroup: [
    { name: 'code', label: 'Code', type: 'text', required: true },
    { name: 'name', label: 'Name', type: 'text', required: true },
    { name: 'specimenTag', label: 'Specimen Tag', type: 'text' },
    { name: 'isActive', label: 'Active', type: 'checkbox' }
  ],
  department: [
    { name: 'code', label: 'Code', type: 'text', required: true },
    { name: 'name', label: 'Name', type: 'text', required: true },
    { name: 'processingCategory', label: 'Processing Category', type: 'select', required: true, help: 'Laboratory: automated or manual laboratory tests. Diagnostic: radiology, sonography, CT, MRI, and similar imaging studies.', options: [
      { value: 'Laboratory', label: 'Laboratory' },
      { value: 'Diagnostic', label: 'Diagnostic' }
    ]}
  ],
  specimen: [
    { name: 'code', label: 'Code', type: 'text', required: true },
    { name: 'name', label: 'Name', type: 'text', required: true },
    { name: 'isActive', label: 'Active', type: 'checkbox' }
  ],
  container: [
    { name: 'code', label: 'Code', type: 'text', required: true },
    { name: 'name', label: 'Name', type: 'text', required: true },
    { name: 'color', label: 'Color', type: 'text' },
    { name: 'isActive', label: 'Active', type: 'checkbox' }
  ],
  testProfile: [
    { name: 'code', label: 'Code', type: 'text', required: true },
    { name: 'name', label: 'Name', type: 'text', required: true },
    { name: 'packageRate', label: 'Package Rate', type: 'number' },
    { name: 'isActive', label: 'Active', type: 'checkbox' }
  ],
  testRate: [
    { name: 'rate', label: 'Rate', type: 'number', required: true },
    { name: 'emergencyRate', label: 'Emergency Rate', type: 'number' },
    { name: 'discountPercent', label: 'Discount %', type: 'number' },
    { name: 'effectiveStart', label: 'Effective From', type: 'date', required: true },
    { name: 'effectiveEnd', label: 'Effective To', type: 'date', required: true },
    { name: 'isActive', label: 'Active', type: 'checkbox' }
  ],
  hisParameter: [
    { name: 'hisParamCode', label: 'Param Code', type: 'text', required: true },
    { name: 'hisParamDescription', label: 'Description', type: 'text', required: true },
    { name: 'hisParamUnit', label: 'Unit', type: 'text' },
    { name: 'hisParamMethod', label: 'Method', type: 'text' },
    { name: 'comments', label: 'Comment', type: 'editor' },
    { name: 'isActive', label: 'Active', type: 'checkbox' }
  ],
  hisParameterRange: [
    { name: 'hisRangeCode', label: 'Range Code', type: 'text', readonly: true },
    { name: 'hisRangeValue', label: 'Range Value', type: 'text' },
    { name: 'gender', label: 'Gender', type: 'select', required: true, options: [
      { value: '', label: '-- Select Gender --' },
      { value: 'Male', label: 'Male' },
      { value: 'Female', label: 'Female' },
      { value: 'Both', label: 'Both' }
    ]},
    { name: 'ageFrom', label: 'Age From', type: 'number' },
    { name: 'ageTo', label: 'Age To', type: 'number' },
    { name: 'ageType', label: 'Age Type', type: 'select', options: [
      { value: '', label: '-- Select Age Type --' },
      { value: 'Year', label: 'Year' },
      { value: 'Month', label: 'Month' }
    ]},
    { name: 'minValue', label: 'Min Value', type: 'number' },
    { name: 'maxValue', label: 'Max Value', type: 'number' }
  ],
  testMapping: [
    { name: 'hisParamCode', label: 'HIS Param Code', type: 'text' },
    { name: 'hisParamDescription', label: 'HIS Param Description', type: 'text' },
    { name: 'lisTestCode', label: 'LIS Param Code', type: 'text', required: true },
    { name: 'lisTestCodeDescription', label: 'LIS Param Description', type: 'text' },
    { name: 'isActive', label: 'Active', type: 'checkbox' }
  ],
  patient: [
    { name: 'hisPatientId', label: 'Patient Number', type: 'text', readonly: true },
    { name: 'patientPrefix', label: 'Salutation', type: 'select', required: true, options: [
      { value: '', label: '-- Select Salutation --' },
      { value: 'Mr.', label: 'Mr.' },
      { value: 'Mrs.', label: 'Mrs.' },
      { value: 'Miss', label: 'Miss' },
      { value: 'Master', label: 'Master' },
      { value: 'Dr.', label: 'Dr.' },
      { value: 'Prof.', label: 'Prof.' }
    ]},
    { name: 'name', label: 'Patient Name', type: 'text', required: true },
    { name: 'mrNo', label: 'MR Number', type: 'text', readonly: true },
    { name: 'visitId', label: 'Visit ID', type: 'text', readonly: true },
    { name: 'phone', label: 'Phone', type: 'text', required: true },
    { name: 'address', label: 'Address', type: 'text' },
    { name: 'gender', label: 'Gender', type: 'select', required: true, options: [
      { value: '', label: '-- Select Gender --' },
      { value: 'M', label: 'Male' },
      { value: 'F', label: 'Female' },
      { value: 'O', label: 'Other' }
    ]},
    { name: 'age', label: 'Age', type: 'number' },
    { name: 'dateOfBirth', label: 'Date of Birth', type: 'date' },
    { name: 'isActive', label: 'Active', type: 'checkbox' }
  ],
  testParameter: [
    { name: 'sequence', label: 'Sequence', type: 'number', required: true, min: 1 },
    { name: 'isActive', label: 'Active', type: 'checkbox' }
  ]
};

const appRoutes: Routes = [
    // Default
    { path: '', component: HomeComponent, pathMatch: 'full', canActivate: ROUTE_GUARDS },

    // Annonimious
    { path: 'about-us', component: AboutComponent },
    { path: 'contact-us', component: ContactComponent },
    { path: 'terms', component: TremsComponent },
    { path: 'help', component: HelpComponent },

    // Authentication
    { path: 'login', component: LoginComponent },
    { path: 'forgot-password', component: ForgotPasswordComponent },
    { path: 'register', component: RegisterComponent },
    { path: 'change-password', component: ChangePasswordComponent },
    { path: 'profile', component: ProfileComponent, canActivate: ROUTE_GUARDS },

    { path: 'equipments/create', component: CreateEquipmentComponent, canActivate: ROUTE_GUARDS },
    { path: 'equipments/edit/:id', component: EditEquipmentComponent, canActivate: ROUTE_GUARDS },
    { path: 'equipments/:id', component: DetailsEquipmentComponent, canActivate: ROUTE_GUARDS },
    { path: 'equipments', component: ListEquipmentComponent, canActivate: ROUTE_GUARDS },
    { path: 'equipment-heartbeat', component: ListEquipmentHeartbeatComponent, canActivate: ROUTE_GUARDS },
    { path: 'parameters/:id', component: DetailsParameterComponent, canActivate: ROUTE_GUARDS },

    { path: 'client-application/create', component: ApplicationCreateComponent, canActivate: ROUTE_GUARDS },
    { path: 'client-application/edit/:id', component: ApplicationEditComponent, canActivate: ROUTE_GUARDS },
    { path: 'client-application/:id', component: ApplicationDetailsComponent, canActivate: ROUTE_GUARDS },
    { path: 'client-application', component: ApplicationListComponent, canActivate: ROUTE_GUARDS },

    { path: 'users/create', component: UsersCreateComponent, canActivate: ROUTE_GUARDS },
    { path: 'users/edit/:id', component: UsersEditComponent, canActivate: ROUTE_GUARDS },
    { path: 'users/:id', component: UsersDetailsComponent, canActivate: ROUTE_GUARDS },
    { path: 'users', component: UsersListComponent, canActivate: ROUTE_GUARDS },

    { path: 'roles/create', component: RolesCreateComponent, canActivate: ROUTE_GUARDS },
    { path: 'roles/edit/:id', component: RolesEditComponent, canActivate: ROUTE_GUARDS },
    { path: 'roles/:id', component: RolesDetailsComponent, canActivate: ROUTE_GUARDS },
    { path: 'roles', component: RolesListComponent, canActivate: ROUTE_GUARDS },

    { path: 'samples', component: ListRawSampleComponent, canActivate: ROUTE_GUARDS },
    { path: 'lab-result-entry', redirectTo: 'samples', pathMatch: 'full' },
    { path: 'lab-result-entry/:sampleNo', component: EditTestResultsComponent, canActivate: ROUTE_GUARDS },
    { path: 'edit-test-results', component: EditTestResultsComponent, canActivate: ROUTE_GUARDS },
    { path: 'edit-test-results/:sampleNo', component: EditTestResultsComponent, canActivate: ROUTE_GUARDS },
    { path: 'samples/create', component: CreateSampleComponent, canActivate: ROUTE_GUARDS },
    { path: 'samples/edit/:id', component: EditSampleComponent, canActivate: ROUTE_GUARDS },
    { path: 'samples/:id', component: RawSampleDetailsComponent, canActivate: ROUTE_GUARDS },

    { path: 'approvedsamples', component: ListApprovedSampleComponent, canActivate: ROUTE_GUARDS },
    { path: 'approved-samples/:id', component: ApprovedSampleDetailsComponent, canActivate: ROUTE_GUARDS },

    { path: 'doctorapprovals', component: ListDoctorSampleComponent, canActivate: ROUTE_GUARDS },
    { path: 'doctor-samples/:id', component: DoctorSampleDetailsComponent, canActivate: ROUTE_GUARDS },

    { path: 'rejectedsamples', component: ListRejectedSampleComponent, canActivate: ROUTE_GUARDS },
    { path: 'rejected-samples/:id', component: RejectedSampleDetailsComponent, canActivate: ROUTE_GUARDS },

    { path: 'technicianapprovals', component: ListTestedSampleComponent, canActivate: ROUTE_GUARDS },
    { path: 'technicianapprovals/:id', component: TechnicianSampleDetailsComponent, canActivate: ROUTE_GUARDS },
    { path: 'technician-samples/:id', component: TechnicianSampleDetailsComponent, canActivate: ROUTE_GUARDS },

    { path: 'quality-controls', component: ListQualitySampleComponent, canActivate: ROUTE_GUARDS },
    { path: 'quality-controls/:id', component: QualityDetailsComponent, canActivate: ROUTE_GUARDS },

    { path: 'test-master', component: TestListComponent, canActivate: ROUTE_GUARDS },
    { path: 'test-master/create', component: TestCreateComponent, canActivate: ROUTE_GUARDS },
    { path: 'test-master/:id', component: TestDetailsComponent, canActivate: ROUTE_GUARDS },
    { path: 'test-master/edit/:id', component: TestEditComponent, canActivate: ROUTE_GUARDS },

    { path: 'departments', component: MasterListComponent, data: { masterKey: 'department' }, canActivate: ROUTE_GUARDS },
    { path: 'departments/create', component: MasterFormComponent, data: { apiName: 'Department', returnUrl: '/departments', title: 'Department', fields: LOOKUP_FIELDS.department }, canActivate: ROUTE_GUARDS },
    { path: 'departments/:id', component: MasterFormComponent, data: { apiName: 'Department', returnUrl: '/departments', title: 'Department', fields: LOOKUP_FIELDS.department }, canActivate: ROUTE_GUARDS },

    { path: 'specimens', component: MasterListComponent, data: { masterKey: 'specimen' }, canActivate: ROUTE_GUARDS },
    { path: 'specimens/create', component: MasterFormComponent, data: { apiName: 'Specimens', returnUrl: '/specimens', title: 'Specimen', fields: LOOKUP_FIELDS.specimen }, canActivate: ROUTE_GUARDS },
    { path: 'specimens/:id', component: MasterFormComponent, data: { apiName: 'Specimens', returnUrl: '/specimens', title: 'Specimen', fields: LOOKUP_FIELDS.specimen }, canActivate: ROUTE_GUARDS },

    { path: 'referral-doctors', component: MasterListComponent, data: { masterKey: 'referralDoctor' }, canActivate: ROUTE_GUARDS },
    { path: 'referral-doctors/create', component: MasterFormComponent, data: { apiName: 'ReferralDoctor', returnUrl: '/referral-doctors', title: 'Referral Doctor', fields: LOOKUP_FIELDS.referral }, canActivate: ROUTE_GUARDS },
    { path: 'referral-doctors/:id', component: MasterFormComponent, data: { apiName: 'ReferralDoctor', returnUrl: '/referral-doctors', title: 'Referral Doctor', fields: LOOKUP_FIELDS.referral }, canActivate: ROUTE_GUARDS },

    { path: 'corporates', component: MasterListComponent, data: { masterKey: 'corporate' }, canActivate: ROUTE_GUARDS },
    { path: 'corporates/create', component: MasterFormComponent, data: { apiName: 'Corporate', returnUrl: '/corporates', title: 'Corporate', fields: LOOKUP_FIELDS.corporate }, canActivate: ROUTE_GUARDS },
    { path: 'corporates/:id', component: MasterFormComponent, data: { apiName: 'Corporate', returnUrl: '/corporates', title: 'Corporate', fields: LOOKUP_FIELDS.corporate }, canActivate: ROUTE_GUARDS },

    { path: 'test-groups', component: MasterListComponent, data: { masterKey: 'testGroup' }, canActivate: ROUTE_GUARDS },
    { path: 'test-groups/create', component: MasterFormComponent, data: { apiName: 'TestGroup', returnUrl: '/test-groups', title: 'Test Group', fields: LOOKUP_FIELDS.testGroup }, canActivate: ROUTE_GUARDS },
    { path: 'test-groups/:id', component: MasterFormComponent, data: { apiName: 'TestGroup', returnUrl: '/test-groups', title: 'Test Group', fields: LOOKUP_FIELDS.testGroup }, canActivate: ROUTE_GUARDS },

    { path: 'test-categories', component: MasterListComponent, data: { masterKey: 'testCategory' }, canActivate: ROUTE_GUARDS },
    { path: 'test-categories/create', component: MasterFormComponent, data: { apiName: 'TestCategory', returnUrl: '/test-categories', title: 'Test Category', fields: LOOKUP_FIELDS.codeName }, canActivate: ROUTE_GUARDS },
    { path: 'test-categories/:id', component: MasterFormComponent, data: { apiName: 'TestCategory', returnUrl: '/test-categories', title: 'Test Category', fields: LOOKUP_FIELDS.codeName }, canActivate: ROUTE_GUARDS },

    { path: 'units', component: MasterListComponent, data: { masterKey: 'unit' }, canActivate: ROUTE_GUARDS },
    { path: 'units/create', component: MasterFormComponent, data: { apiName: 'Unit', returnUrl: '/units', title: 'Unit', fields: LOOKUP_FIELDS.codeName }, canActivate: ROUTE_GUARDS },
    { path: 'units/:id', component: MasterFormComponent, data: { apiName: 'Unit', returnUrl: '/units', title: 'Unit', fields: LOOKUP_FIELDS.codeName }, canActivate: ROUTE_GUARDS },

    { path: 'methods', component: MasterListComponent, data: { masterKey: 'method' }, canActivate: ROUTE_GUARDS },
    { path: 'methods/create', component: MasterFormComponent, data: { apiName: 'Method', returnUrl: '/methods', title: 'Method', fields: LOOKUP_FIELDS.method }, canActivate: ROUTE_GUARDS },
    { path: 'methods/:id', component: MasterFormComponent, data: { apiName: 'Method', returnUrl: '/methods', title: 'Method', fields: LOOKUP_FIELDS.method }, canActivate: ROUTE_GUARDS },

    { path: 'sample-types', component: MasterListComponent, data: { masterKey: 'sampleType' }, canActivate: ROUTE_GUARDS },
    { path: 'sample-types/create', component: MasterFormComponent, data: { apiName: 'SampleType', returnUrl: '/sample-types', title: 'Sample Type', fields: LOOKUP_FIELDS.codeName }, canActivate: ROUTE_GUARDS },
    { path: 'sample-types/:id', component: MasterFormComponent, data: { apiName: 'SampleType', returnUrl: '/sample-types', title: 'Sample Type', fields: LOOKUP_FIELDS.codeName }, canActivate: ROUTE_GUARDS },

    { path: 'containers', component: MasterListComponent, data: { masterKey: 'container' }, canActivate: ROUTE_GUARDS },
    { path: 'containers/create', component: MasterFormComponent, data: { apiName: 'Container', returnUrl: '/containers', title: 'Container', fields: LOOKUP_FIELDS.container }, canActivate: ROUTE_GUARDS },
    { path: 'containers/:id', component: MasterFormComponent, data: { apiName: 'Container', returnUrl: '/containers', title: 'Container', fields: LOOKUP_FIELDS.container }, canActivate: ROUTE_GUARDS },

    { path: 'test-profiles', component: MasterListComponent, data: { masterKey: 'testProfile' }, canActivate: ROUTE_GUARDS },
    { path: 'test-profiles/create', component: TestProfileFormComponent, canActivate: ROUTE_GUARDS },
    { path: 'test-profiles/edit/:id', component: TestProfileFormComponent, canActivate: ROUTE_GUARDS },
    { path: 'test-profiles/:id', component: TestProfileViewComponent, canActivate: ROUTE_GUARDS },

    { path: 'test-rates', component: MasterListComponent, data: { masterKey: 'testRate' }, canActivate: ROUTE_GUARDS },
    { path: 'test-rates/create', component: MasterFormComponent, data: { apiName: 'TestRate', returnUrl: '/test-rates', title: 'Test Rate', fields: LOOKUP_FIELDS.testRate }, canActivate: ROUTE_GUARDS },
    { path: 'test-rates/:id', component: MasterFormComponent, data: { apiName: 'TestRate', returnUrl: '/test-rates', title: 'Test Rate', fields: LOOKUP_FIELDS.testRate }, canActivate: ROUTE_GUARDS },

    { path: 'his-parameters', component: MasterListComponent, data: { masterKey: 'hisParameter' }, canActivate: ROUTE_GUARDS },
    { path: 'his-parameters/create', component: MasterFormComponent, data: { apiName: 'HisParameterMaster', returnUrl: '/his-parameters', title: 'Parameter', fields: LOOKUP_FIELDS.hisParameter }, canActivate: ROUTE_GUARDS },
    { path: 'his-parameters/:id', component: MasterFormComponent, data: { apiName: 'HisParameterMaster', returnUrl: '/his-parameters', title: 'Parameter', fields: LOOKUP_FIELDS.hisParameter }, canActivate: ROUTE_GUARDS },

    { path: 'his-parameter-ranges', component: MasterListComponent, data: { masterKey: 'hisParameterRange' }, canActivate: ROUTE_GUARDS },
    { path: 'his-parameter-ranges/create', component: MasterFormComponent, data: { apiName: 'HisParameterRangeMaster', returnUrl: '/his-parameter-ranges', title: 'Parameter Range', fields: LOOKUP_FIELDS.hisParameterRange }, canActivate: ROUTE_GUARDS },
    { path: 'his-parameter-ranges/:id', component: MasterFormComponent, data: { apiName: 'HisParameterRangeMaster', returnUrl: '/his-parameter-ranges', title: 'Parameter Range', fields: LOOKUP_FIELDS.hisParameterRange }, canActivate: ROUTE_GUARDS },

    { path: 'test-mappings', component: MasterListComponent, data: { masterKey: 'testMapping' }, canActivate: ROUTE_GUARDS },
    { path: 'test-mappings/create', component: MasterFormComponent, data: { apiName: 'TestMappingMaster', returnUrl: '/test-mappings', title: 'Analyzer Parameter Mapping', fields: LOOKUP_FIELDS.testMapping }, canActivate: ROUTE_GUARDS },
    { path: 'test-mappings/:id', component: MasterFormComponent, data: { apiName: 'TestMappingMaster', returnUrl: '/test-mappings', title: 'Analyzer Parameter Mapping', fields: LOOKUP_FIELDS.testMapping }, canActivate: ROUTE_GUARDS },
    { path: 'analyzer-parameter-mappings', redirectTo: 'test-mappings', pathMatch: 'full' },

    { path: 'test-parameters', component: MasterListComponent, data: { masterKey: 'testParameter' }, canActivate: ROUTE_GUARDS },
    { path: 'test-parameters/create', component: MasterFormComponent, data: { apiName: 'TestParameterMappingMaster', returnUrl: '/test-parameters', title: 'Test Parameter Mapping', fields: LOOKUP_FIELDS.testParameter }, canActivate: ROUTE_GUARDS },
    { path: 'test-parameters/:id', component: MasterFormComponent, data: { apiName: 'TestParameterMappingMaster', returnUrl: '/test-parameters', title: 'Test Parameter Mapping', fields: LOOKUP_FIELDS.testParameter }, canActivate: ROUTE_GUARDS },

    { path: 'patient-master', component: MasterListComponent, data: { masterKey: 'patientMaster' }, canActivate: ROUTE_GUARDS },
    { path: 'patient-master/create', component: MasterFormComponent, data: { apiName: 'PatientMaster', returnUrl: '/patient-master', title: 'Patient', fields: LOOKUP_FIELDS.patient }, canActivate: ROUTE_GUARDS },
    { path: 'patient-master/:id', component: MasterFormComponent, data: { apiName: 'PatientMaster', returnUrl: '/patient-master', title: 'Patient', fields: LOOKUP_FIELDS.patient }, canActivate: ROUTE_GUARDS },

    { path: 'sale-invoices', component: MasterListComponent, data: { masterKey: 'saleInvoice' }, canActivate: ROUTE_GUARDS },
    { path: 'sale-invoices/create', component: SaleInvoiceFormComponent, canActivate: ROUTE_GUARDS },
    { path: 'sale-invoices/print/:id', component: SaleInvoiceFormComponent, canActivate: ROUTE_GUARDS },
    { path: 'sale-invoices/:id', component: SaleInvoiceFormComponent, canActivate: ROUTE_GUARDS },

    { path: 'reports/sale-invoice-register', component: SaleInvoiceRegisterComponent, canActivate: ROUTE_GUARDS },
    { path: 'reports/test-booking-register', component: TestBookingRegisterComponent, canActivate: ROUTE_GUARDS },
    { path: 'reports/test-report', component: TestReportComponent, canActivate: ROUTE_GUARDS },
    { path: 'reports/radiology-report', component: RadiologyReportPrintComponent, canActivate: ROUTE_GUARDS },

    { path: 'sample-collection', component: SampleCollectionComponent, canActivate: ROUTE_GUARDS },
    { path: 'sample-receiving', component: SampleReceivingComponent, canActivate: ROUTE_GUARDS },
    { path: 'radiology-report-entry', component: RadiologyReportEntryComponent, canActivate: ROUTE_GUARDS },
    { path: 'radiology-doctor-approvals', component: RadiologyDoctorApprovalComponent, canActivate: ROUTE_GUARDS },
    { path: 'radiology-approved-reports', component: RadiologyApprovedReportsComponent, canActivate: ROUTE_GUARDS },

    { path: 'reports/collection-summary', component: FddReportComponent, data: {
      title: 'Collection Summary', endpoint: 'CollectionSummary', defaultSort: 'CollectionDate', exportName: 'CollectionSummary',
      columns: [
        { header: 'Collection Date', field: 'collectionDate', type: 'date' },
        { header: 'Barcode', field: 'sampleNo' }, { header: 'Order No.', field: 'orderNumber' },
        { header: 'Patient ID', field: 'patientId' }, { header: 'Patient', field: 'patientName' },
        { header: 'Test', field: 'testName' }, { header: 'Collector', field: 'collectedBy' }, { header: 'Status', field: 'status' }
      ]
    }, canActivate: ROUTE_GUARDS },
    { path: 'reports/collector-wise', component: FddReportComponent, data: {
      title: 'Collector Wise Report', endpoint: 'CollectorWise', defaultSort: 'CollectorName', exportName: 'CollectorWise', showPatientFilter: false, showCollectorFilter: true,
      columns: [
        { header: 'Collector', field: 'collectorName' }, { header: 'Collected', field: 'totalCollected' },
        { header: 'Rejected', field: 'totalRejected' }, { header: 'Recollection', field: 'totalRecollection' }
      ]
    }, canActivate: ROUTE_GUARDS },
    { path: 'reports/pending-collection', component: FddReportComponent, data: {
      title: 'Pending Collection Report', endpoint: 'PendingCollection', defaultSort: 'OrderDate', exportName: 'PendingCollection', showPatientFilter: false,
      columns: [
        { header: 'Barcode', field: 'sampleNo' }, { header: 'Order No.', field: 'orderNumber' },
        { header: 'Patient ID', field: 'patientId' }, { header: 'Patient', field: 'patientName' },
        { header: 'Test', field: 'testName' }, { header: 'Order Date', field: 'orderDate', type: 'date' }
      ]
    }, canActivate: ROUTE_GUARDS },
    { path: 'reports/recollection', component: FddReportComponent, data: {
      title: 'Recollection Report', endpoint: 'Recollection', defaultSort: 'CollectionDate', exportName: 'Recollection',
      columns: [
        { header: 'Barcode', field: 'sampleNo' }, { header: 'Order', field: 'orderNumber' },
        { header: 'Patient', field: 'patientName' }, { header: 'Test', field: 'testName' },
        { header: 'Collector', field: 'collectedBy' }, { header: 'Date', field: 'collectionDate', type: 'datetime' }, { header: 'Remarks', field: 'remarks' }
      ]
    }, canActivate: ROUTE_GUARDS },
    { path: 'reports/received-samples', component: FddReportComponent, data: {
      title: 'Received Samples Report', endpoint: 'ReceivedSamples', defaultSort: 'ReceivedDate', exportName: 'ReceivedSamples',
      columns: [
        { header: 'Barcode', field: 'sampleNo' }, { header: 'Patient', field: 'patientName' }, { header: 'Test', field: 'testName' },
        { header: 'Collected', field: 'collectionDate', type: 'datetime' }, { header: 'Received', field: 'receivedDate', type: 'datetime' },
        { header: 'Received By', field: 'receivedBy' }, { header: 'TAT (min)', field: 'turnaroundMinutes' }
      ]
    }, canActivate: ROUTE_GUARDS },
    { path: 'reports/rejected-samples', component: FddReportComponent, data: {
      title: 'Rejected Samples Report', endpoint: 'RejectedSamples', defaultSort: 'RejectedOn', exportName: 'RejectedSamples',
      columns: [
        { header: 'Barcode', field: 'sampleNo' }, { header: 'Patient', field: 'patientName' }, { header: 'Test', field: 'testName' },
        { header: 'Stage', field: 'stage' }, { header: 'Reason', field: 'rejectionReason' },
        { header: 'Rejected By', field: 'rejectedBy' }, { header: 'Rejected On', field: 'rejectedOn', type: 'datetime' }
      ]
    }, canActivate: ROUTE_GUARDS },
    { path: 'reports/sample-turnaround', component: FddReportComponent, data: {
      title: 'Turnaround Time Report', endpoint: 'SampleTurnaround', defaultSort: 'ReceivedDate', exportName: 'SampleTurnaround',
      columns: [
        { header: 'Barcode', field: 'sampleNo' }, { header: 'Patient', field: 'patientName' }, { header: 'Test', field: 'testName' },
        { header: 'Collection', field: 'collectionDate', type: 'datetime' }, { header: 'Received', field: 'receivedDate', type: 'datetime' },
        { header: 'TAT (min)', field: 'turnaroundMinutes' }
      ]
    }, canActivate: ROUTE_GUARDS },
    { path: 'reports/radiology/pending', component: FddReportComponent, data: {
      title: 'Pending Reporting Cases', endpoint: 'PendingRadiology', defaultSort: 'CreatedOn', exportName: 'PendingRadiology', showModalityFilter: true,
      columns: [
        { header: 'Accession', field: 'accessionNo' }, { header: 'Patient', field: 'patientName' }, { header: 'Test', field: 'testName' },
        { header: 'Modality', field: 'modality' }, { header: 'Status', field: 'status' }, { header: 'Created', field: 'createdOn', type: 'datetime' }
      ]
    }, canActivate: ROUTE_GUARDS },
    { path: 'reports/radiology/authorized', component: FddReportComponent, data: {
      title: 'Authorized Reports', endpoint: 'AuthorizedRadiology', defaultSort: 'AuthorizedOn', exportName: 'AuthorizedRadiology', showModalityFilter: true,
      columns: [
        { header: 'Accession', field: 'accessionNo' }, { header: 'Patient', field: 'patientName' }, { header: 'Test', field: 'testName' },
        { header: 'Modality', field: 'modality' }, { header: 'Authorized By', field: 'authorizedBy' },
        { header: 'Authorized On', field: 'authorizedOn', type: 'datetime' }, { header: 'Status', field: 'status' }
      ]
    }, canActivate: ROUTE_GUARDS },
    { path: 'reports/radiology/modality-stats', component: FddReportComponent, data: {
      title: 'Modality Statistics', endpoint: 'ModalityStatistics', defaultSort: 'Modality', exportName: 'ModalityStatistics', showPatientFilter: false,
      columns: [
        { header: 'Modality', field: 'modality' }, { header: 'Total', field: 'totalCases' },
        { header: 'Authorized', field: 'authorizedCases' }, { header: 'Pending', field: 'pendingCases' }
      ]
    }, canActivate: ROUTE_GUARDS },
    { path: 'reports/radiology/productivity', component: FddReportComponent, data: {
      title: 'Radiologist Productivity Report', endpoint: 'RadiologistProductivity', defaultSort: 'RadiologistName', exportName: 'RadiologistProductivity', showPatientFilter: false,
      columns: [
        { header: 'Radiologist', field: 'radiologistName' }, { header: 'Authorized', field: 'authorizedCount' }, { header: 'Released', field: 'releasedCount' }
      ]
    }, canActivate: ROUTE_GUARDS },

    // otherwise redirect to home
    { path: '**', redirectTo: '' }
];

export const routing = RouterModule.forRoot(appRoutes, {
    onSameUrlNavigation: 'reload',
    relativeLinkResolution: 'legacy'
});
