import { Component, OnInit, Input } from '@angular/core';
import { ModuleService } from '../../_services';
import { DomSanitizer } from '@angular/platform-browser';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-media-list-view',
  templateUrl: './media-list-view.component.html',
  styleUrls: ['./media-list-view.component.css']
})
export class MediaListViewComponent implements OnInit {
  @Input() MediaId: string;
  @Input() events: Observable<void>;
  items: any[];
  isLoaded: boolean = false;
  option: any;

  constructor(private moduleService: ModuleService,
    private sanitizer: DomSanitizer) { }

  ngOnInit() {
    this.option = {
      'RecordPerPage': 10,
      'CurrentPage': 1,
      'SortColumnName': 'Id',
      'SortDirection': false,
      'SearchCondition': {
        'Name': 'MediaId',
        'Value': this.MediaId
      }
    };

    this.getItems();
    if (this.events != null) {
      this.events.subscribe(() => this.getItems());
    }
  }

  getItems() {

    this.moduleService.getItems('MediaFile', this.option)
      .subscribe(response => {
        this.items = response.items;
        this.isLoaded = true;
      });
  }

  images = [1, 2, 3, 4].map((n) => `/assets/images/slide-${n}.jpg`);

  trustUrl(url: string) {
    return this.sanitizer.bypassSecurityTrustUrl(url);
  }

  deleteFile(id) {
    var Ids = [];
    Ids.push(id);

    this.moduleService.deleteItem('MediaFile', Ids, 0)
      .subscribe(response => {
        this.getItems();
      });
  }

  mediaDocumentIcon(ext: string) {
    ext = ext.replace('.', '');
    let path = `/assets/images/${ext}.png`;
    return this.trustUrl(path);
  }

  formatVideoThumb(url: string) {
    return this.trustUrl(this.moduleService.thumbVideo(url));
  }

  
  mediaType(ext: string) {
    return this.moduleService.mediaType(ext);
  }
}
