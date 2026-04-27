import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { AvailableAppComponent } from './available-app.component';

describe('AvailableAppComponent', () => {
  let component: AvailableAppComponent;
  let fixture: ComponentFixture<AvailableAppComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ AvailableAppComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(AvailableAppComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
