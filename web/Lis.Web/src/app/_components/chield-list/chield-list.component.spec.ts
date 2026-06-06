import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ChieldListComponent } from './chield-list.component';

describe('ChieldListComponent', () => {
  let component: ChieldListComponent;
  let fixture: ComponentFixture<ChieldListComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ChieldListComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ChieldListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
