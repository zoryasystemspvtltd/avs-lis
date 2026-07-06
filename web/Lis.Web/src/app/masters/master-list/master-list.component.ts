import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router, NavigationEnd } from '@angular/router';
import { Subscription } from 'rxjs';
import { filter } from 'rxjs/operators';
import { MASTER_LIST_SCHEMAS } from '../master-schemas';

@Component({
  selector: 'app-master-list',
  template: `
    <br>
    <app-list-module *ngIf="listRenderKey && moduleJson" [schemma]="moduleJson" [listModuleKey]="listRenderKey"></app-list-module>
    <br>
  `
})
export class MasterListComponent implements OnInit, OnDestroy {
  moduleJson: any;
  listRenderKey: string = null;
  private currentKey: string;
  private routeSub: Subscription;
  private routerSub: Subscription;

  constructor(private route: ActivatedRoute, private router: Router) { }

  ngOnInit() {
    this.routeSub = this.route.data.subscribe(data => {
      this.loadSchema(data['masterKey']);
    });
    this.routerSub = this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      const key = this.route.snapshot.data['masterKey'];
      if (key) {
        this.loadSchema(key);
      }
    });
  }

  ngOnDestroy() {
    if (this.routeSub) {
      this.routeSub.unsubscribe();
    }
    if (this.routerSub) {
      this.routerSub.unsubscribe();
    }
  }

  private loadSchema(key: string) {
    if (!key || !MASTER_LIST_SCHEMAS[key]) {
      return;
    }
    if (key === this.currentKey && this.listRenderKey) {
      return;
    }
    this.currentKey = key;
    this.listRenderKey = null;
    this.moduleJson = Object.assign({
      allowPaging: true,
      hideSearch: false
    }, MASTER_LIST_SCHEMAS[key]);
    setTimeout(() => {
      this.listRenderKey = key;
    }, 0);
  }
}
