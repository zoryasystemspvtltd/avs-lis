import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AlertService, MasterService } from '../../_services';

@Component({
  selector: 'app-test-profile-view',
  templateUrl: './test-profile-view.component.html'
})
export class TestProfileViewComponent implements OnInit {
  loading = true;
  profile: any;
  id: string;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private masterService: MasterService,
    private alertService: AlertService) { }

  ngOnInit() {
    this.id = this.route.snapshot.params['id'];
    this.masterService.getProfileHierarchy(this.id).subscribe(
      data => {
        this.profile = data;
        this.loading = false;
      },
      () => {
        this.loading = false;
        this.alertService.error('Unable to load test profile.');
      }
    );
  }

  edit() {
    this.router.navigate(['/test-profiles/edit', this.id]);
  }

  back() {
    this.router.navigate(['/test-profiles']);
  }

  deactivate() {
    if (!confirm('Deactivate this test profile?')) { return; }
    this.masterService.deleteItem('TestProfile', { id: +this.id }).subscribe(
      () => {
        this.alertService.success('Profile deactivated');
        this.router.navigate(['/test-profiles']);
      },
      () => this.alertService.error('Deactivate failed')
    );
  }
}
