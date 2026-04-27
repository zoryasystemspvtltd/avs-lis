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


    // otherwise redirect to home
    { path: '**', redirectTo: '' }
];

export const routing = RouterModule.forRoot(appRoutes);
