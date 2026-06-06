import { Component, Input, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthenticationToken } from '../../../_models';
import { UserService, AuthenticationService, EquipmentService } from '../../../_services';

@Component({
  selector: 'app-his-test-details',
  templateUrl: './his-test-details.component.html',
  styleUrls: ['./his-test-details.component.css']
})
export class HisTestDetailsComponent implements OnInit {
  item: any;
  isLoaded: boolean = false;
  @Input() hisTestCode: string;

  constructor(private equipmentService: EquipmentService,
    private authenticationService: AuthenticationService,
    private route: ActivatedRoute,
    private router: Router) { }

  ngOnInit() {
    this.getItemDetails();
  }

  getItemDetails() {
    this.equipmentService.getHisTest(this.hisTestCode)
      .subscribe(response => {
        this.item = response;
        this.item.parameters.forEach(p => {
          this.equipmentService.getHisParameterRangesByParameterId(p.id)
            .subscribe(par => {
              p.ranges = par;
              this.isLoaded = true;
            })
        })
      });
  }

}
