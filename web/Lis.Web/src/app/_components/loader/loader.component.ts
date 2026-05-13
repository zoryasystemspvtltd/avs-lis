import { Component, OnInit } from '@angular/core';
      import { LoaderService } from '../../_services/loader.service';
 
      @Component({
        selector: 'app-loading',
        templateUrl: './loader.component.html',
        styleUrls: ['./loader.component.css']
      })
      export class LoaderComponent implements OnInit {
 
        loading: boolean;
        constructor(private loaderService: LoaderService) {
          this.loaderService.isLoading.subscribe((v) => {
            //console.log(v);
            this.loading = v;
          });
        }
        ngOnInit() {
        }
 
      }