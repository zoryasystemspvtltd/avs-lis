import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { AuthenticationService } from './authentication.service';
import { map, mergeMap } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class ParameterService {
    constructor(private http: HttpClient, private authenticationService: AuthenticationService) { }

    getById(id: string) {
        return this.http.get<any>(`${environment.ApplicationServer}/api/tests/${id}`)
            .pipe(map(response => {
                return response;
            })
                , mergeMap(equipment => this.getMappingById(id)
                    .pipe(map(response => {
                        equipment.mappings = response;
                        return equipment;
                    }))))

    }

    getMappingById(id: string) {
        return this.http.get<any>(`${environment.ApplicationServer}/api/EquipmentParamMappings/${id}`)
            .pipe(map(response => {
                return response;
            }))
    }

    
}