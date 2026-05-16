import { Routes, RouterModule } from '@angular/router';
import { AuthGuard } from './_guards';

import { HomeComponent } from './home/home.component';
import { AboutComponent, ContactComponent, TremsComponent } from './annonimious';
import { LoginComponent, ForgotPasswordComponent, RegisterComponent, ChangePasswordComponent } from './authentication';
import { ProfileComponent } from './authentication/profile/profile.component';
import { ApplicationCreateComponent, ApplicationEditComponent, ApplicationDetailsComponent, ApplicationListComponent } from './administration/applications';
import { UsersListComponent, UsersCreateComponent, UsersEditComponent, UsersDetailsComponent } from './administration/users';
import { RolesListComponent, RolesCreateComponent, RolesEditComponent, RolesDetailsComponent } from './administration/roles';
import { MediaFileListComponent } from './_components';
import { CreateEquipmentComponent, EditEquipmentComponent, ListEquipmentComponent, DetailsEquipmentComponent, ListRawSampleComponent, ListApprovedSampleComponent, ListDoctorSampleComponent, ListRejectedSampleComponent, ListTestedSampleComponent, TechnicianSampleDetailsComponent, RawSampleDetailsComponent, TechnicianSampleSearchComponent, ListQualitySampleComponent, QualityDetailsComponent } from './LIS';
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
import { MasterListComponent, MasterFormComponent, SaleInvoiceFormComponent, TestProfileFormComponent } from './masters';
import { ListEquipmentHeartbeatComponent } from './LIS';

const LOOKUP_FIELDS = {
  codeName: [
    { name: 'code', label: 'Code', type: 'text', required: true },
    { name: 'name', label: 'Name', type: 'text', required: true },
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
    { name: 'name', label: 'Name', type: 'text', required: true }
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
    { name: 'taxPercent', label: 'Tax %', type: 'number' },
    { name: 'effectiveStart', label: 'Effective From', type: 'date', required: true },
    { name: 'effectiveEnd', label: 'Effective To', type: 'date', required: true },
    { name: 'isActive', label: 'Active', type: 'checkbox' }
  ],
  hisParameter: [
    { name: 'hisParamCode', label: 'Param Code', type: 'text', required: true },
    { name: 'hisParamDescription', label: 'Description', type: 'text', required: true },
    { name: 'hisParamUnit', label: 'Unit', type: 'text' },
    { name: 'hisParamMethod', label: 'Method', type: 'text' },
    { name: 'lisParamCode', label: 'LIS Param Code', type: 'text' }
  ],
  hisParameterRange: [
    { name: 'hisRangeCode', label: 'Range Code', type: 'text', required: true },
    { name: 'hisRangeValue', label: 'Range Value', type: 'text' },
    { name: 'gender', label: 'Gender', type: 'text' },
    { name: 'ageFrom', label: 'Age From', type: 'number' },
    { name: 'ageTo', label: 'Age To', type: 'number' },
    { name: 'ageType', label: 'Age Type', type: 'text' },
    { name: 'minValue', label: 'Min Value', type: 'number' },
    { name: 'maxValue', label: 'Max Value', type: 'number' }
  ],
  testMapping: [
    { name: 'hisTestCode', label: 'Test Code', type: 'text', required: true },
    { name: 'hisTestCodeDescription', label: 'Test Description', type: 'text' },
    { name: 'lisTestCode', label: 'LIS Test Code', type: 'text', required: true },
    { name: 'lisTestCodeDescription', label: 'LIS Test Description', type: 'text' },
    { name: 'specimenCode', label: 'Specimen Code', type: 'text' },
    { name: 'isActive', label: 'Active', type: 'checkbox' }
  ],
  patient: [
    { name: 'name', label: 'Patient Name', type: 'text', required: true },
    { name: 'hisPatientId', label: 'External Patient ID', type: 'text' },
    { name: 'phone', label: 'Phone', type: 'text' },
    { name: 'gender', label: 'Gender', type: 'text' },
    { name: 'age', label: 'Age', type: 'number' },
    { name: 'dateOfBirth', label: 'Date of Birth', type: 'date' },
    { name: 'isActive', label: 'Active', type: 'checkbox' }
  ],
  testParameter: [
    { name: 'hisParamCode', label: 'Parameter Code', type: 'text', required: true },
    { name: 'hisParamDescription', label: 'Parameter Name', type: 'text', required: true },
    { name: 'hisParamUnit', label: 'Unit', type: 'text' },
    { name: 'hisParamMethod', label: 'Method', type: 'text' },
    { name: 'lisParamCode', label: 'LIS Param Code', type: 'text' }
  ]
};

const appRoutes: Routes = [
    // Default
    { path: '', component: HomeComponent, pathMatch: 'full', canActivate: [AuthGuard] },

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
    { path: 'profile', component: ProfileComponent, canActivate: [AuthGuard] },

    { path: 'equipments/create', component: CreateEquipmentComponent, canActivate: [AuthGuard] },
    { path: 'equipments/edit/:id', component: EditEquipmentComponent, canActivate: [AuthGuard] },
    { path: 'equipments/:id', component: DetailsEquipmentComponent, canActivate: [AuthGuard] },
    { path: 'equipments', component: ListEquipmentComponent, canActivate: [AuthGuard] },
    { path: 'equipment-heartbeat', component: ListEquipmentHeartbeatComponent, canActivate: [AuthGuard] },
    { path: 'parameters/:id', component: DetailsParameterComponent, canActivate: [AuthGuard] },

    { path: 'client-application/create', component: ApplicationCreateComponent, canActivate: [AuthGuard] },
    { path: 'client-application/edit/:id', component: ApplicationEditComponent, canActivate: [AuthGuard] },
    { path: 'client-application/:id', component: ApplicationDetailsComponent, canActivate: [AuthGuard] },
    { path: 'client-application', component: ApplicationListComponent, canActivate: [AuthGuard] },

    { path: 'users/create', component: UsersCreateComponent, canActivate: [AuthGuard] },
    { path: 'users/edit/:id', component: UsersEditComponent, canActivate: [AuthGuard] },
    { path: 'users/:id', component: UsersDetailsComponent, canActivate: [AuthGuard] },
    { path: 'users', component: UsersListComponent, canActivate: [AuthGuard] },

    { path: 'roles/create', component: RolesCreateComponent, canActivate: [AuthGuard] },
    { path: 'roles/edit/:id', component: RolesEditComponent, canActivate: [AuthGuard] },
    { path: 'roles/:id', component: RolesDetailsComponent, canActivate: [AuthGuard] },
    { path: 'roles', component: RolesListComponent, canActivate: [AuthGuard] },

    { path: 'samples', component: ListRawSampleComponent, canActivate: [AuthGuard] },
    { path: 'samples/create', component: CreateSampleComponent, canActivate: [AuthGuard] },
    { path: 'samples/edit/:id', component: EditSampleComponent, canActivate: [AuthGuard] },
    { path: 'samples/:id', component: RawSampleDetailsComponent, canActivate: [AuthGuard] },

    { path: 'approvedsamples', component: ListApprovedSampleComponent, canActivate: [AuthGuard] },
    { path: 'approved-samples/:id', component: ApprovedSampleDetailsComponent, canActivate: [AuthGuard] },

    { path: 'doctorapprovals', component: ListDoctorSampleComponent, canActivate: [AuthGuard] },
    { path: 'doctor-samples/:id', component: DoctorSampleDetailsComponent, canActivate: [AuthGuard] },

    { path: 'rejectedsamples', component: ListRejectedSampleComponent, canActivate: [AuthGuard] },
    { path: 'rejected-samples/:id', component: RejectedSampleDetailsComponent, canActivate: [AuthGuard] },

    { path: 'technicianapprovals', component: ListTestedSampleComponent, canActivate: [AuthGuard] },
    { path: 'technician-samples/:id', component: TechnicianSampleDetailsComponent, canActivate: [AuthGuard] },

    { path: 'quality-controls', component: ListQualitySampleComponent, canActivate: [AuthGuard] },
    { path: 'quality-controls/:id', component: QualityDetailsComponent, canActivate: [AuthGuard] },

    { path: 'test-master', component: TestListComponent, canActivate: [AuthGuard] },
    { path: 'test-master/create', component: TestCreateComponent, canActivate: [AuthGuard] },
    { path: 'test-master/:id', component: TestDetailsComponent, canActivate: [AuthGuard] },
    { path: 'test-master/edit/:id', component: TestEditComponent, canActivate: [AuthGuard] },

    { path: 'departments', component: MasterListComponent, data: { masterKey: 'department' }, canActivate: [AuthGuard] },
    { path: 'departments/create', component: MasterFormComponent, data: { apiName: 'Department', returnUrl: '/departments', title: 'Department', fields: LOOKUP_FIELDS.department }, canActivate: [AuthGuard] },
    { path: 'departments/:id', component: MasterFormComponent, data: { apiName: 'Department', returnUrl: '/departments', title: 'Department', fields: LOOKUP_FIELDS.department }, canActivate: [AuthGuard] },

    { path: 'specimens', component: MasterListComponent, data: { masterKey: 'specimen' }, canActivate: [AuthGuard] },
    { path: 'specimens/create', component: MasterFormComponent, data: { apiName: 'Specimens', returnUrl: '/specimens', title: 'Specimen', fields: LOOKUP_FIELDS.specimen }, canActivate: [AuthGuard] },
    { path: 'specimens/:id', component: MasterFormComponent, data: { apiName: 'Specimens', returnUrl: '/specimens', title: 'Specimen', fields: LOOKUP_FIELDS.specimen }, canActivate: [AuthGuard] },

    { path: 'referral-doctors', component: MasterListComponent, data: { masterKey: 'referralDoctor' }, canActivate: [AuthGuard] },
    { path: 'referral-doctors/create', component: MasterFormComponent, data: { apiName: 'ReferralDoctor', returnUrl: '/referral-doctors', title: 'Referral Doctor', fields: LOOKUP_FIELDS.referral }, canActivate: [AuthGuard] },
    { path: 'referral-doctors/:id', component: MasterFormComponent, data: { apiName: 'ReferralDoctor', returnUrl: '/referral-doctors', title: 'Referral Doctor', fields: LOOKUP_FIELDS.referral }, canActivate: [AuthGuard] },

    { path: 'corporates', component: MasterListComponent, data: { masterKey: 'corporate' }, canActivate: [AuthGuard] },
    { path: 'corporates/create', component: MasterFormComponent, data: { apiName: 'Corporate', returnUrl: '/corporates', title: 'Corporate', fields: LOOKUP_FIELDS.corporate }, canActivate: [AuthGuard] },
    { path: 'corporates/:id', component: MasterFormComponent, data: { apiName: 'Corporate', returnUrl: '/corporates', title: 'Corporate', fields: LOOKUP_FIELDS.corporate }, canActivate: [AuthGuard] },

    { path: 'test-groups', component: MasterListComponent, data: { masterKey: 'testGroup' }, canActivate: [AuthGuard] },
    { path: 'test-groups/create', component: MasterFormComponent, data: { apiName: 'TestGroup', returnUrl: '/test-groups', title: 'Test Group', fields: LOOKUP_FIELDS.testGroup }, canActivate: [AuthGuard] },
    { path: 'test-groups/:id', component: MasterFormComponent, data: { apiName: 'TestGroup', returnUrl: '/test-groups', title: 'Test Group', fields: LOOKUP_FIELDS.testGroup }, canActivate: [AuthGuard] },

    { path: 'test-categories', component: MasterListComponent, data: { masterKey: 'testCategory' }, canActivate: [AuthGuard] },
    { path: 'test-categories/create', component: MasterFormComponent, data: { apiName: 'TestCategory', returnUrl: '/test-categories', title: 'Test Category', fields: LOOKUP_FIELDS.codeName }, canActivate: [AuthGuard] },
    { path: 'test-categories/:id', component: MasterFormComponent, data: { apiName: 'TestCategory', returnUrl: '/test-categories', title: 'Test Category', fields: LOOKUP_FIELDS.codeName }, canActivate: [AuthGuard] },

    { path: 'units', component: MasterListComponent, data: { masterKey: 'unit' }, canActivate: [AuthGuard] },
    { path: 'units/create', component: MasterFormComponent, data: { apiName: 'Unit', returnUrl: '/units', title: 'Unit', fields: LOOKUP_FIELDS.codeName }, canActivate: [AuthGuard] },
    { path: 'units/:id', component: MasterFormComponent, data: { apiName: 'Unit', returnUrl: '/units', title: 'Unit', fields: LOOKUP_FIELDS.codeName }, canActivate: [AuthGuard] },

    { path: 'methods', component: MasterListComponent, data: { masterKey: 'method' }, canActivate: [AuthGuard] },
    { path: 'methods/create', component: MasterFormComponent, data: { apiName: 'Method', returnUrl: '/methods', title: 'Method', fields: LOOKUP_FIELDS.codeName }, canActivate: [AuthGuard] },
    { path: 'methods/:id', component: MasterFormComponent, data: { apiName: 'Method', returnUrl: '/methods', title: 'Method', fields: LOOKUP_FIELDS.codeName }, canActivate: [AuthGuard] },

    { path: 'sample-types', component: MasterListComponent, data: { masterKey: 'sampleType' }, canActivate: [AuthGuard] },
    { path: 'sample-types/create', component: MasterFormComponent, data: { apiName: 'SampleType', returnUrl: '/sample-types', title: 'Sample Type', fields: LOOKUP_FIELDS.codeName }, canActivate: [AuthGuard] },
    { path: 'sample-types/:id', component: MasterFormComponent, data: { apiName: 'SampleType', returnUrl: '/sample-types', title: 'Sample Type', fields: LOOKUP_FIELDS.codeName }, canActivate: [AuthGuard] },

    { path: 'containers', component: MasterListComponent, data: { masterKey: 'container' }, canActivate: [AuthGuard] },
    { path: 'containers/create', component: MasterFormComponent, data: { apiName: 'Container', returnUrl: '/containers', title: 'Container', fields: LOOKUP_FIELDS.container }, canActivate: [AuthGuard] },
    { path: 'containers/:id', component: MasterFormComponent, data: { apiName: 'Container', returnUrl: '/containers', title: 'Container', fields: LOOKUP_FIELDS.container }, canActivate: [AuthGuard] },

    { path: 'test-profiles', component: MasterListComponent, data: { masterKey: 'testProfile' }, canActivate: [AuthGuard] },
    { path: 'test-profiles/create', component: TestProfileFormComponent, canActivate: [AuthGuard] },
    { path: 'test-profiles/:id', component: TestProfileFormComponent, canActivate: [AuthGuard] },

    { path: 'test-rates', component: MasterListComponent, data: { masterKey: 'testRate' }, canActivate: [AuthGuard] },
    { path: 'test-rates/create', component: MasterFormComponent, data: { apiName: 'TestRate', returnUrl: '/test-rates', title: 'Test Rate', fields: LOOKUP_FIELDS.testRate }, canActivate: [AuthGuard] },
    { path: 'test-rates/:id', component: MasterFormComponent, data: { apiName: 'TestRate', returnUrl: '/test-rates', title: 'Test Rate', fields: LOOKUP_FIELDS.testRate }, canActivate: [AuthGuard] },

    { path: 'his-parameters', component: MasterListComponent, data: { masterKey: 'hisParameter' }, canActivate: [AuthGuard] },
    { path: 'his-parameters/create', component: MasterFormComponent, data: { apiName: 'HisParameterMaster', returnUrl: '/his-parameters', title: 'Parameter', fields: LOOKUP_FIELDS.hisParameter }, canActivate: [AuthGuard] },
    { path: 'his-parameters/:id', component: MasterFormComponent, data: { apiName: 'HisParameterMaster', returnUrl: '/his-parameters', title: 'Parameter', fields: LOOKUP_FIELDS.hisParameter }, canActivate: [AuthGuard] },

    { path: 'his-parameter-ranges', component: MasterListComponent, data: { masterKey: 'hisParameterRange' }, canActivate: [AuthGuard] },
    { path: 'his-parameter-ranges/create', component: MasterFormComponent, data: { apiName: 'HisParameterRangeMaster', returnUrl: '/his-parameter-ranges', title: 'Parameter Range', fields: LOOKUP_FIELDS.hisParameterRange }, canActivate: [AuthGuard] },
    { path: 'his-parameter-ranges/:id', component: MasterFormComponent, data: { apiName: 'HisParameterRangeMaster', returnUrl: '/his-parameter-ranges', title: 'Parameter Range', fields: LOOKUP_FIELDS.hisParameterRange }, canActivate: [AuthGuard] },

    { path: 'test-mappings', component: MasterListComponent, data: { masterKey: 'testMapping' }, canActivate: [AuthGuard] },
    { path: 'test-mappings/create', component: MasterFormComponent, data: { apiName: 'TestMappingMaster', returnUrl: '/test-mappings', title: 'Test Mapping', fields: LOOKUP_FIELDS.testMapping }, canActivate: [AuthGuard] },
    { path: 'test-mappings/:id', component: MasterFormComponent, data: { apiName: 'TestMappingMaster', returnUrl: '/test-mappings', title: 'Test Mapping', fields: LOOKUP_FIELDS.testMapping }, canActivate: [AuthGuard] },

    { path: 'test-parameters', component: MasterListComponent, data: { masterKey: 'testParameter' }, canActivate: [AuthGuard] },
    { path: 'test-parameters/create', component: MasterFormComponent, data: { apiName: 'HisParameterMaster', returnUrl: '/test-parameters', title: 'Test Parameter Mapping', fields: LOOKUP_FIELDS.testParameter }, canActivate: [AuthGuard] },
    { path: 'test-parameters/:id', component: MasterFormComponent, data: { apiName: 'HisParameterMaster', returnUrl: '/test-parameters', title: 'Test Parameter Mapping', fields: LOOKUP_FIELDS.testParameter }, canActivate: [AuthGuard] },

    { path: 'patient-master', component: MasterListComponent, data: { masterKey: 'patientMaster' }, canActivate: [AuthGuard] },
    { path: 'patient-master/create', component: MasterFormComponent, data: { apiName: 'PatientMaster', returnUrl: '/patient-master', title: 'Patient', fields: LOOKUP_FIELDS.patient }, canActivate: [AuthGuard] },
    { path: 'patient-master/:id', component: MasterFormComponent, data: { apiName: 'PatientMaster', returnUrl: '/patient-master', title: 'Patient', fields: LOOKUP_FIELDS.patient }, canActivate: [AuthGuard] },

    { path: 'sale-invoices', component: MasterListComponent, data: { masterKey: 'saleInvoice' }, canActivate: [AuthGuard] },
    { path: 'sale-invoices/create', component: SaleInvoiceFormComponent, canActivate: [AuthGuard] },
    { path: 'sale-invoices/print/:id', component: SaleInvoiceFormComponent, canActivate: [AuthGuard] },
    { path: 'sale-invoices/:id', component: SaleInvoiceFormComponent, canActivate: [AuthGuard] },

    // otherwise redirect to home
    { path: '**', redirectTo: '' }
];

export const routing = RouterModule.forRoot(appRoutes, {
    onSameUrlNavigation: 'reload',
    relativeLinkResolution: 'legacy'
});
