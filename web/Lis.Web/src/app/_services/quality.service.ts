import { Injectable } from '@angular/core';
import { HttpClient,HttpHeaders } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { map, mergeMap } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class QualityService {
    constructor(private http: HttpClient) { }

    getAll(option:any) {      

        const requestOptions = {                                                                                                                                                                                 
            headers: new HttpHeaders({ 'ApiOption':JSON.stringify(option)}), 
          };

        return this.http.get<any>(`${environment.ApplicationServer}/api/quality/`,requestOptions)
        .pipe(map(response => {
            //console.log(response);

            return response;
        }));
       
    }

    getQualityResult(id: number) {
        return this.http.get<any>(`${environment.ApplicationServer}/api/quality/${id}`)
            .pipe(map(response => {
                return response;
            }));
    }
   
    getQualityData(paramCode: string) {
        return this.http.get<any>(`${environment.ApplicationServer}/api/quality/${paramCode}`)
            .pipe(map(response => {
                let item = response;
                return item;
            }));
    }

}