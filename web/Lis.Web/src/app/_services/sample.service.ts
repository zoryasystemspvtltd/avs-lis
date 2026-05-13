import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { AuthenticationService } from './authentication.service';
import { map, mergeMap } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class SampleService {
    constructor(private http: HttpClient, private authenticationService: AuthenticationService) { }

    getHisSamples() {
        return this.http.get<any>(`${environment.ApplicationServer}/api/Hospitals`)
            .pipe(map(response => {
                return response;
            }));
    }

    getSample(id: number) {
        return this.http.get<any>(`${environment.ApplicationServer}/api/rawsample/${id}`)
            .pipe(map(response => {
                return response;
            }), mergeMap(result => this.getBarcode(result.sampleNo)
                .pipe(map(response => {
                    result.barcodeText = `${response}`;
                    return result;
                }))), mergeMap(result => this.getSampleParameters(result.id)
                    .pipe(map(response => {
                        result.parameters = response;
                        return result;
                    }))));
    }

    getBarcode(id: string) {
        return this.http.get<any>(`${environment.ApplicationServer}/api/barcode/${id}`)
            .pipe(map(response => {
                return response;
            }));
    }

    getSampleParameters(id: number) {
        return this.http.get<any>(`${environment.ApplicationServer}/api/rawparameters/${id}`)
            .pipe(map(response => {
                return response;
            }));
    }

    getResultDetails(id: number) {
        return this.http.get<any>(`${environment.ApplicationServer}/api/sampledetails/${id}`)
            .pipe(map(response => {
                return response;
            }));
    }

    getResult(id: number) {
        return this.http.get<any>(`${environment.ApplicationServer}/api/sample/${id}`)
            .pipe(map(response => {
                if(!response){return null;}
                let item = response.test;
                item.testRuns = response.testRuns;
                return item;
            }));
    }

    getSearchResult(id: string) {
        return this.http.get<any>(`${environment.ApplicationServer}/api/SampleSearch/${id}`)
            .pipe(map(response => {
                let item = response;
                return item;
            }));
    }

    reviewSample(item: any) {
        return this.http.post<any>(`${environment.ApplicationServer}/api/sample/`, item)
            .pipe(map(response => {
                return response;
            }));
    }

    authorizeSample(item: any) {
        return this.http.put<any>(`${environment.ApplicationServer}/api/sample/`, item)
            .pipe(map(response => {
                return response;
            }));
    }

    createNewSample(item: any) {
        return this.http.post<any>(`${environment.ApplicationServer}/api/newsample/`, item)
            .pipe(map(response => {
                return response;
            }));
    }

    public print(printEl: HTMLElement) {
        let printContainer: HTMLElement = document.querySelector('#print-container');

        if (!printContainer) {
            printContainer = document.createElement('div');
            printContainer.id = 'print-container';
        }

        printContainer.innerHTML = '';

        let elementCopy = printEl.cloneNode(true);
        printContainer.appendChild(elementCopy);

        const popupWin = window.open('', '_blank', 'top=0,left=0,height=100%,width=auto');
        popupWin.document.body.appendChild(printContainer);

        popupWin.window.print();

        popupWin.document.close();
    }
}

