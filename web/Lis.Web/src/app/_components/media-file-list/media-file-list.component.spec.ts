import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { MediaFileListComponent } from './media-file-list.component';

describe('MediaFileListComponent', () => {
  let component: MediaFileListComponent;
  let fixture: ComponentFixture<MediaFileListComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ MediaFileListComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MediaFileListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
