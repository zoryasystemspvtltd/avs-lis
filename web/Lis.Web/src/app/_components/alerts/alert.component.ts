import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subscription } from 'rxjs';

import { AlertService } from '../../_services';
import { DomSanitizer } from '@angular/platform-browser';

@Component({
    selector: 'alert',
    templateUrl: 'alert.component.html'
})

export class AlertComponent implements OnInit, OnDestroy {
    private subscription: Subscription;
    message: any;

    constructor(private alertService: AlertService,private sanitizer: DomSanitizer) { }

    ngOnInit() {
        this.subscription = this.alertService.getMessage().subscribe(message => { 
            //message.text = this.sanitizer.bypassSecurityTrustHtml(message.text);
            this.message = message; 
        });
    }

    ngOnDestroy() {
        if(this.subscription != undefined){
            this.subscription.unsubscribe();
        }
    }

    close() {
        this.message = '';
    }
}