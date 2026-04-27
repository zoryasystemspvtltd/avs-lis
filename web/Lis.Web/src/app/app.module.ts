import { BrowserModule } from '@angular/platform-browser';
import { ReactiveFormsModule } from '@angular/forms';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { NgxMaskModule, IConfig } from 'ngx-mask'
import { routing } from './app.routing';
import { JwtInterceptor, ErrorInterceptor } from './_helpers';
import { NgSelectModule } from '@ng-select/ng-select';
import { EditorModule } from '@tinymce/tinymce-angular';
import { LoaderService } from './_services/loader.service';
import { LoaderInterceptor } from './_helpers/loader.interceptor';
import {NgbModule} from '@ng-bootstrap/ng-bootstrap';
import { AppComponent } from './app.component';
import { HeaderComponent, NavMenuComponent, FooterComponent, CompanyInfoComponent, AlertComponent, YouFrameComponent, NgbdAlertCloseable, LeftNavMenuComponent, AvailableAppComponent, TinyMceEditorComponent, CreateModuleComponent, ListModuleComponent, NgbdModalComponent, EditModuleComponent, ViewModuleComponent, LoaderComponent, FileuploadComponent, MediaListViewComponent, ChieldListComponent, MediaFileListComponent } from './_components';
import { HomeComponent } from './home/home.component';
import { LoginComponent, ChangePasswordComponent, ForgotPasswordComponent, RegisterComponent } from './authentication';
import { AboutComponent, ContactComponent, TremsComponent } from './annonimious';
import { LogListComponent } from './activitylog/log-list/log-list.component';
import { StatusTypePipe, ActivityTypePipe } from './_pipes';
import { ProfileComponent } from './authentication/profile/profile.component';
import { NgxBarcodeModule } from 'ngx-barcode';

import { ApplicationListComponent, ApplicationCreateComponent, ApplicationEditComponent, ApplicationDetailsComponent } from './administration/applications';
import { UsersListComponent, UsersDetailsComponent, UsersCreateComponent, UsersEditComponent } from './administration/users';
import { RolesListComponent, RolesCreateComponent, RolesEditComponent, RolesDetailsComponent } from './administration/roles';
import { environment } from '../environments/environment';
import { ListEquipmentComponent, CreateEquipmentComponent, EditEquipmentComponent,  EditTestMappingComponent,  DetailsEquipmentComponent, ListRawSampleComponent, ListApprovedSampleComponent, ListDoctorSampleComponent, ListRejectedSampleComponent, ListTestedSampleComponent, TechnicianSampleDetailsComponent, RawSampleChartComponent, ProcessedSampleChartComponent, DoctorSampleChartComponent, HisSampleComponent,RawSampleDetailsComponent, HisTestDetailsComponent, TechnicianSampleSearchComponent, ListQualitySampleComponent, QualityDetailsComponent } from './LIS';
import { CreateParameterComponent} from './LIS/EquipmentParamMapping/create-parameter/create-parameter.component';
import { EditParammeterComponent } from './LIS/EquipmentParamMapping/edit-parameter/edit-parameter.component';
import { DetailsParameterComponent } from './LIS/EquipmentParamMapping/details-parameter/details-parameter.component';
import { NgxEchartsModule } from 'ngx-echarts';
import { StatusChartComponent } from './LIS/chart/status-chart/status-chart.component';
import { DoctorSampleDetailsComponent } from './LIS/samples/doctor-details/sample-details.component';
import { HelpComponent } from './annonimious/help/help.component';
import { ApprovedSampleDetailsComponent } from './LIS/samples/approved-sample/sample-details.component';
import { RejectedSampleDetailsComponent } from './LIS/samples/rejected-sample/sample-details.component';
import { NgMultiSelectDropDownModule } from 'ng-multiselect-dropdown';
import { CreateSampleComponent } from './LIS/samples/create-sample/create-sample.component';
import { BarcodeComponent } from './LIS/barcode/barcode.component';
import { NgxPrintModule } from 'ngx-print';
import { LeveyJenningChartComponent } from './LIS/chart/levey-jenning-chart/levey-jenning-chart.component';
import { EditSampleComponent } from './LIS/samples/edit-sample/edit-sample.component';
import { AlertEquipmentComponent } from './LIS/Equipment/list-equipment/alert-equipment.component';


export const options: Partial<IConfig> | (() => Partial<IConfig>) = null;

@NgModule({
  declarations: [
    AppComponent,
    HeaderComponent,
    NavMenuComponent,
    FooterComponent,
    HomeComponent,
    LoginComponent,
    CompanyInfoComponent,
    AboutComponent,
    HelpComponent,
    ContactComponent,
    TremsComponent,
    ChangePasswordComponent,
    ForgotPasswordComponent,
    AlertComponent,
    YouFrameComponent,
    RegisterComponent,
    NgbdAlertCloseable,
    LeftNavMenuComponent,
    AvailableAppComponent,
    StatusTypePipe,
    ActivityTypePipe,
    TinyMceEditorComponent,
    NgbdModalComponent,
    CreateModuleComponent,
    ListModuleComponent,
    EditModuleComponent,
    ViewModuleComponent,
    LogListComponent,
    ProfileComponent,
    UsersListComponent,
    UsersDetailsComponent,
    UsersCreateComponent,
    UsersEditComponent,
    ApplicationListComponent,
    ApplicationCreateComponent,
    ApplicationEditComponent,
    ApplicationDetailsComponent,
    RolesListComponent,
    RolesCreateComponent,
    RolesEditComponent,
    RolesDetailsComponent,
    LoaderComponent,
    FileuploadComponent,
    MediaListViewComponent,
    ChieldListComponent,
    MediaFileListComponent,
    ListEquipmentComponent,
    CreateEquipmentComponent,
    EditEquipmentComponent,
    DetailsEquipmentComponent,
    AlertEquipmentComponent,
    EditTestMappingComponent,
    TechnicianSampleDetailsComponent,
    DoctorSampleDetailsComponent,
    CreateParameterComponent,
    EditParammeterComponent,
    DetailsParameterComponent,
    ListRawSampleComponent,
    ListApprovedSampleComponent,
    ListDoctorSampleComponent,
    ListRejectedSampleComponent,
    ListTestedSampleComponent,
    RawSampleChartComponent,
    ProcessedSampleChartComponent,
    DoctorSampleChartComponent,
    StatusChartComponent,
    HisSampleComponent,
    RawSampleDetailsComponent,
    HisTestDetailsComponent,
    ApprovedSampleDetailsComponent,
    RejectedSampleDetailsComponent,
    CreateSampleComponent,
    TechnicianSampleSearchComponent,
    ListQualitySampleComponent,
    QualityDetailsComponent,
    BarcodeComponent,
    LeveyJenningChartComponent,
    EditSampleComponent
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    NgxEchartsModule.forRoot({
      echarts: () => import('echarts')
    }),
    ReactiveFormsModule,
    HttpClientModule,
    FormsModule,
    routing,
    NgxMaskModule.forRoot(options),
    NgSelectModule,
    EditorModule,
    NgbModule,
    NgMultiSelectDropDownModule.forRoot(),
    NgxBarcodeModule,
    NgxPrintModule
  ],
  providers: [
    { provide: HTTP_INTERCEPTORS, useClass: JwtInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true },
    LoaderService,
    { provide: HTTP_INTERCEPTORS, useClass: LoaderInterceptor, multi: true }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
