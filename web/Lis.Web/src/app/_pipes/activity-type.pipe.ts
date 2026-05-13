import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'activityType'
})
export class ActivityTypePipe implements PipeTransform {

  transform(value: number): string {
    switch (value) {
      case 1:
          return 'Modified';
      case 2:
          return 'Marked as Published';
      case 3:
          return 'Marked as Archived';
      case 4:
          return 'Deleted';
      default:
          return 'Added';
    }
  }

}
