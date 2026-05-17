import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AlertService } from '../../../_services';
import { RateMasterService } from '../../../_services/rate-master.service';

@Component({
  selector: 'app-rate-master-details',
  templateUrl: './rate-master-details.component.html',
  styleUrls: ['./rate-master-details.component.css']
})
export class RateMasterDetailsComponent implements OnInit, OnDestroy {
  id: string;
  private sub: any;
  public item: any;
  public isLoaded = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private rateMasterService: RateMasterService,
    private alertService: AlertService
  ) { }

  ngOnInit() {
    this.sub = this.route.params.subscribe(params => {
      this.id = params['id'];
      this.loadItem();
    });
  }

  loadItem() {
    this.rateMasterService.getById(this.id)
      .subscribe(response => {
        this.item = response;
        this.isLoaded = true;
      },
      error => {
        this.alertService.error(error?.error?.message || 'Failed to load rate master details');
        this.isLoaded = true;
      });
  }

  deleteItem() {
    if (confirm('Are you sure you want to delete this rate master record?')) {
      this.rateMasterService.delete(this.id)
        .subscribe(() => {
          this.alertService.success('Rate master record deleted successfully');
          this.router.navigate(['/rate-master']);
        },
        error => {
          this.alertService.error(error?.error?.message || 'Failed to delete rate master record');
        });
    }
  }

  ngOnDestroy() {
    this.sub.unsubscribe();
  }
}
