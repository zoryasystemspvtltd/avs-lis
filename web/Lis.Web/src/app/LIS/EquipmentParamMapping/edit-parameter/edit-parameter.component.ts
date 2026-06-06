import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-edit-parameter',
  templateUrl: './edit-parameter.component.html',
  styleUrls: ['./edit-parameter.component.css']
})
export class EditParammeterComponent implements OnInit {

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
