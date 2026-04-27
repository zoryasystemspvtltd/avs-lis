import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { MediaListViewComponent } from './media-list-view.component';

describe('MediaListViewComponent', () => {
  let component: MediaListViewComponent;
  let fixture: ComponentFixture<MediaListViewComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ MediaListViewComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MediaListViewComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
