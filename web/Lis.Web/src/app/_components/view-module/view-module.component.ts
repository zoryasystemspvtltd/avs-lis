import { Component, OnInit, Input } from '@angular/core';
import { AuthenticationToken } from '../../_models';
import { ModuleService, AlertService, AuthenticationService } from '../../_services';
import { FormBuilder } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-view-module',
  templateUrl: './view-module.component.html',
  styleUrls: ['./view-module.component.css']
})
export class ViewModuleComponent implements OnInit {
  id: string;
  private user:AuthenticationToken;
  @Input() schemma:any;
  public item:any;
  private sub: any;
  public isLoaded:Boolean;

  constructor(private moduleService: ModuleService,
    private formBuilder: FormBuilder,
    private alertService: AlertService,
    private authenticationService: AuthenticationService,
    private route: ActivatedRoute) { }

  ngOnInit() {
    this.user = this.authenticationService.currentUserValue;

    this.sub = this.route.params.subscribe(params => {
      this.isLoaded = false;
      this.id = params['id']; // (+) converts string 'id' to a number
      // In a real app: dispatch action to load the details here.
      this.getItemDetails(this.id);
    });
  }

  hasAccess=function(type:number,status:number){
    
    let _hasAccess:boolean=false;
    let acc = this.user.access.find(element =>element.name == this.schemma.module);

    if (acc == null) {
      _hasAccess = false;
    }

    _hasAccess = ((parseInt(acc.access) & type) == type);
    
    if(this.item.status == status){
      _hasAccess = false;
    }

    if(this.schemma.hideAction && (status == 2 || status ==3)){
      _hasAccess = false;
    }
    return _hasAccess;
  }

  getItemDetails(id:string){
    this.moduleService.getItem(this.schemma.module,id)
    .subscribe(response => { 
      this.item = response.items[0];
      this.isLoaded = true;
    });
  }

  editItem(status:number){
    var Ids = [];
    Ids.push(this.id);

    this.moduleService.deleteItem(this.schemma.module,Ids, status)
        .subscribe(response => { 
          this.getItemDetails(this.id);
        });
  }
}
