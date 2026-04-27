import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { User, ResetPassword, ChangePassword, UserInfo } from '../_models';
import { environment } from '../../environments/environment';
import { AuthenticationService } from './authentication.service';
import { map, mergeMap } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class EquipmentService {
    constructor(private http: HttpClient, private authenticationService: AuthenticationService) { }

    getAll() {
        return this.http.get<User[]>(`${environment.ApplicationServer}/api/Equipments/`)
            .pipe(map(response => {
                //console.log(response);

                return response;
            }));
    }

    getById(id: string) {
        return this.http.get<any>(`${environment.ApplicationServer}/api/Equipments/${id}`)
            .pipe(map(response => {
                return response;
            }),
                mergeMap(equipment => this.getMappingById(id)
                    .pipe(map(response => {
                        equipment.mappings = response;
                        equipment.mappings.forEach(m => {
                            if (m.hisTestCode) {
                                m.showDetails = false;
                            }
                        })
                        return equipment;
                    }))));

    }

    getModels() {
        return this.http.get<any>(`${environment.ApplicationServer}/api/EquipmentTestMappings/`)
            .pipe(map(response => {
                return response;
            }))
    }

    getDepartments() {
        return this.http.get<User[]>(`${environment.ApplicationServer}/api/Department/`)
            .pipe(map(response => {
                return response;
            }));
    }

    getMappingById(id: string) {
        return this.http.get<any>(`${environment.ApplicationServer}/api/EquipmentTestMappings/${id}`)
            .pipe(map(response => {
                return response;
            }))
    }

    getLisTestById(id: string) {
        return this.http.get<any>(`${environment.ApplicationServer}/api/LisTest/${id}`)
            .pipe(map(response => {
                return response;
            }))
    }

    addEquipment(item: any) {
        return this.http.post<any>(`${environment.ApplicationServer}/api/Equipments/`, item)
            .pipe(map(response => {
                //console.log(response);

                return response;
            }));
    }

    editEquipment(item: any) {
        return this.http.put<any>(`${environment.ApplicationServer}/api/Equipments/${item.id}`, item)
            .pipe(map(response => {
                //console.log(response);

                return response;
            }));
    }

    changeMappings(equipmentId: number, mappings: any[]) {
        var equipment = {
            equipmentId: equipmentId,
            mappings: mappings
        }
        return this.http.post<any>(`${environment.ApplicationServer}/api/EquipmentTestMappings`, equipment)
            .pipe(map(response => {
                //console.log(response);

                return response;
            }));
    }

    getEquipmentDailyStatus(type: number) {
        return this.http.get<any[]>(`${environment.ApplicationServer}/api/DailyStatus/${type}`)
            .pipe(map(response => {
                //console.log(response);

                return response;
            }));
    }
    getLisTests() {
        return this.http.get<User[]>(`${environment.ApplicationServer}/api/listest/`)
            .pipe(map(response => {
                return response;
            }));
    }

    getHisTests() {
        return this.http.get<User[]>(`${environment.ApplicationServer}/api/histest/`)
            .pipe(map(response => {
                return response;
            }));
    }

    getHisTest(id: string) {
        return this.http.get<any>(`${environment.ApplicationServer}/api/histest/${id}`)
            .pipe(map(response => {
                //console.log(response);

                return response;
            }),
                mergeMap(test => this.getHisParameterByTestId(test.id)
                    .pipe(map(response => {
                        test.parameters = response;
                        return test;
                    }))));
    }

    getHisParameterByTestId(id: number) {
        return this.http.get<any[]>(`${environment.ApplicationServer}/api/hisparameter/${id}`)
            .pipe(map(response => {
                //console.log(response);

                return response;
            }));
    }

    getHisParameterRangesByParameterId(id: number) {
        return this.http.get<any[]>(`${environment.ApplicationServer}/api/hisparameterrange/${id}`)
            .pipe(map(response => {
                //console.log(response);

                return response;
            }));
    }
}