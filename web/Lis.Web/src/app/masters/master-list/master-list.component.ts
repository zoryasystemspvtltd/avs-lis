import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MASTER_LIST_SCHEMAS } from '../master-schemas';

@Component({
  selector: 'app-master-list',
  template: '<br><app-list-module [schemma]="moduleJson"></app-list-module><br>'
})
export class MasterListComponent implements OnInit {
  moduleJson: any;

  constructor(private route: ActivatedRoute) { }

  ngOnInit() {
    const key = this.route.snapshot.data['masterKey'];
    this.moduleJson = Object.assign({}, MASTER_LIST_SCHEMAS[key]);
    if (this.moduleJson.hideCreate === undefined) {
      this.moduleJson.hideCreate = false;
    }
  }
}
