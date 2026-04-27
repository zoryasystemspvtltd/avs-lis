import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams, HttpErrorResponse } from '@angular/common/http';

import { User, ResetPassword, ChangePassword, UserInfo } from '../_models';
import { environment } from '../../environments/environment';
import { AuthenticationService } from './authentication.service';
import { map, catchError } from 'rxjs/operators';
import { throwError, Observable, BehaviorSubject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ModuleService {

    public tempData: any;

    constructor(private http: HttpClient, private authenticationService: AuthenticationService) { }

    //public apiName:string;
    getItems(apiName: string, option: any) {
        const requestOptions = {
            headers: new HttpHeaders({ 'ApiOption': JSON.stringify(option) }),
        };

        return this.http.get<any>(`${environment.ApplicationServer}/api/${apiName}/`, requestOptions)
            .pipe(map(response => {
                return response;
            }));
    }

    getItem(apiName: string, id: string) {
        let option = {
            'RecordPerPage': 1,
            'CurrentPage': 1,
            'SortColumnName': 'Name',
            'SortDirection': true,
            'SearchCondition': {
                'Name': 'Id',
                'Value': id
            }
        };

        const requestOptions = {
            headers: new HttpHeaders({ 'ApiOption': JSON.stringify(option) }),
        };

        return this.http.get<any>(`${environment.ApplicationServer}/api/${apiName}/`, requestOptions)
            .pipe(map(response => {
                //console.log(response);

                return response;
            }));
    }



    addItem(apiName: string, item: any) {
        return this.http.post<any>(`${environment.ApplicationServer}/api/${apiName}/`, item)
            .pipe(map(response => {
                //console.log(response);

                return response;
            }));
    }

    editItem(apiName: string, item: any) {
        return this.http.put<any>(`${environment.ApplicationServer}/api/${apiName}/`, item)
            .pipe(map(response => {
                //console.log(response);

                return response;
            }));
    }

    deleteItem(apiName: string, ids: string[], state: number) {
        const requestOptions = {
            headers: new HttpHeaders(),
            params: new HttpParams()
                .set('state', state.toString())
                .set('Ids', JSON.stringify(ids)),
        };

        return this.http.delete<any>(`${environment.ApplicationServer}/api/${apiName}/`, requestOptions)
            .pipe(map(response => {
                //console.log(response);

                return response;
            }));

    }

    setTempData(data: any) {
        this.tempData = data;
    }

    getTempData() {
        return this.tempData;
    }


    parseQueryString(queryString: string) {
        let data: any = {},
            pairs, separatorIndex, escapedKey, escapedValue, key, value;

        if (queryString === null) {
            return data;
        }

        pairs = queryString.split('&');

        for (const pair of pairs) {

            separatorIndex = pair.indexOf('=');

            if (separatorIndex === -1) {
                escapedKey = pair;
                escapedValue = null;
            } else {
                escapedKey = pair.substr(0, separatorIndex);
                escapedValue = pair.substr(separatorIndex + 1);
            }

            key = decodeURIComponent(escapedKey);
            value = decodeURIComponent(escapedValue);

            data[key] = value;
        }

        return data;
    }

    thumbVideo(url:string){
        if (url.indexOf('youtu.be') > 0) {
            let fileName = url.substring(url.lastIndexOf('/'));
            fileName = fileName.replace('/', '');
            let thumb = `http://img.youtube.com/vi/${fileName}/0.jpg`;
            return thumb;
          }
      
          if (url.indexOf('youtube.com') > 0) {
            
            let fileName = url.substring(url.lastIndexOf('?'));
            fileName = fileName.replace('?', '');
            let querys = this.parseQueryString(fileName);
      
            let mediaName = querys.v;
      
            let thumb = `http://img.youtube.com/vi/${mediaName}/0.jpg`;
            return thumb;
          }
    }

    embedVideoUrl(url:string){
        if (url.indexOf('youtu.be') > 0) {
            let fileName = url.substring(url.lastIndexOf('/'));
            fileName = fileName.replace('/', '');
            let thumb = `https://www.youtube.com/embed/${fileName}`;
            return thumb;
          }
      
          if (url.indexOf('youtube.com') > 0) {
            
            let fileName = url.substring(url.lastIndexOf('?'));
            fileName = fileName.replace('?', '');
            let querys = this.parseQueryString(fileName);
      
            let mediaName = querys.v;
      
            let thumb = `https://www.youtube.com/embed/${fileName}`;
            return thumb;
          }
    }

    mediaType(ext: string) {
        switch (ext) {
          case 'jpg':
          case '.jpeg':
          case 'gif':
          case 'png':
            return 1; // Image
          case 'pdf':
          case 'doc':
          case 'docx':
          case 'ppt':
          case 'pptx':
          case 'xls':
          case 'xlsx':
            return 2; // Document
          case 'http':
            return 3;
          default:
            return 2;
        }
      }
}