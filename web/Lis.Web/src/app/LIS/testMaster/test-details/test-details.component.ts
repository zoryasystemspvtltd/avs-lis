import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup } from '@angular/forms';
import { AlertService,TestMasterService } from '../../../_services';

@Component({
  selector: 'app-test-details',
  templateUrl: './test-details.component.html',
  styleUrls: ['./test-details.component.css']
})
export class TestDetailsComponent implements OnInit {

  id: string;
  private sub: any;
  public isLoaded: boolean;
  item: any;

  constructor(
    private testMasterService: TestMasterService,
    private route: ActivatedRoute,
    private formBuilder: FormBuilder,
    private alertService: AlertService,
    private router: Router) { }

  ngOnInit() {
    this.sub = this.route.params.subscribe(params => {
      this.id = params['id'];
      this.loadTestData();
    });
  }

  loadTestData() {
    this.testMasterService.getById(this.id)
      .subscribe(response => {
        this.item = response;
        this.isLoaded = true;
      },
      error => {
        this.alertService.error('Failed to load test details');
        this.isLoaded = true;
      });
  }

  deleteTest() {
    if (confirm('Are you sure you want to delete this test?')) {
      this.testMasterService.delete(this.id)
        .subscribe(
          data => {
            this.alertService.success('Test deleted successfully');
            this.router.navigate(['/test-master']);
          },
          error => {
            this.alertService.error(error?.error?.message || 'Failed to delete test');
          });
    }
  }

  ngOnDestroy() {
    this.sub.unsubscribe();
  }
}
