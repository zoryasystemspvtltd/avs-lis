import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { TremsComponent } from './trems.component';

describe('TremsComponent', () => {
  let component: TremsComponent;
  let fixture: ComponentFixture<TremsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ TremsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(TremsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
