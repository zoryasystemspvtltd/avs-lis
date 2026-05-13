import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { YouFrameComponent } from './you-frame.component';

describe('YouFrameComponent', () => {
  let component: YouFrameComponent;
  let fixture: ComponentFixture<YouFrameComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ YouFrameComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(YouFrameComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
