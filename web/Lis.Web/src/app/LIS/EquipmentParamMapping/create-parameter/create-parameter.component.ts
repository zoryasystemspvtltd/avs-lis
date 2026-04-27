import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-create-parameter',
  templateUrl: './create-parameter.component.html',
  styleUrls: ['./create-parameter.component.css']
})
export class CreateParameterComponent implements OnInit {

  public isLoaded: Boolean;
  
  submitted: boolean = false;
  constructor() { }

  ngOnInit(): void {
    this.isLoaded = false;
  }
  isInValid(field: string) {
    if (this.submitted) {
      // if (this.f[field].errors && this.f[field].errors.required) {
      //   return true;
      // }
    }
    return false;
  }

}
