import { Component, OnInit, Input } from '@angular/core';
import { IDropdownSettings, } from 'ng-multiselect-dropdown';
import { EquipmentService } from '../../../_services';

@Component({
  selector: 'app-edit-test-mapping',
  templateUrl: './edit-test-mapping.component.html',
  styleUrls: ['./edit-test-mapping.component.css']
})
export class EditTestMappingComponent implements OnInit {
  public isLoaded: Boolean;
  @Input() mappings: any[];
  @Input() model: string;
  lisTests: any[];
  departments: any[];
  dropdownSettings = {};
  constructor(private equipmentService: EquipmentService) { }

  ngOnInit(): void {
    this.isLoaded = false;
    this.getDepartments();
    this.getLisTests();
    this.dropdownSettings = {
      singleSelection: false,
      idField: 'code',
      textField: 'description',
      itemsShowLimit: 3,
      allowSearchFilter: true,
      enableCheckAll: false
    };

    for (let map of this.mappings) {
      if (map.lisTests && map.lisTests.length > 0) {
        map.selectedTests = map.lisTests;
        if (map.lisTests.length === 1 && map.lisTests[0].code === null) {
          map.selectedTests = null;
        }
      }
      else
        map.selectedTests = null;
    }
  }

  getDepartments() {
    this.equipmentService.getDepartments()
      .subscribe(response => {
        this.departments = response;
        this.isLoaded = true;
      });
  }

  onDepartmentSelected(e) {
    console.log("the selected value is " + e.target.value);
  }
  getLisTests() {
    this.equipmentService.getLisTestById(this.model)
      .subscribe(response => {
        this.lisTests = response;
        this.isLoaded = true;
      });
  }

  onCheckActive(event, element) {
    element.isActive = event.target.checked;
  }

  isInValid(mapping: any) {
    if (mapping.isActive && (mapping.lisTests === null || mapping.lisTests.length === 0)) {
      return true;
    }
    return false;
  }

  hasAccess(mode) {
    return true;
  }

  filterMapping(mappings:any[], code:string){
    let filteredMapping = mappings.filter(m => m.departmentCode == code);
    return filteredMapping;
  }

  selectPanel(item){
    item.selected = !item.selected
  }

}
