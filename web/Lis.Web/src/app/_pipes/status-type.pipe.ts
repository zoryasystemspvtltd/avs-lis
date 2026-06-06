import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'statusType'
})
export class StatusTypePipe implements PipeTransform {

  transform(value: number): string {
    switch (value) {
      case 1:
        return "<span class='glyphicon glyphicon-blackboard' title='Sent To Equipment' ></span>";//"Sent To Equipment";
      case 2:
        return "<span class='glyphicon glyphicon-book' title='Report Generated' ></span>";//"Report Generated";
      case 3:
        return "<span class='glyphicon glyphicon-eye-open' title='Technician Approved' ></span>";//Archived
      case -3:
          return "<span class='glyphicon glyphicon-sunglasses' title='2nd Opinion requested' ></span>";//Archived
      case 4:
        return "<span class='glyphicon glyphicon-eye-close' title='Technician Rejected' ></span>";//Archived
      case 5:
        return "<span class='glyphicon glyphicon-thumbs-up' title='Doctor Approved' ></span>";//Archived
      case 6:
        return "<span class='glyphicon glyphicon-thumbs-down' title='Doctor Rejected' ></span>";//Archived
      case 7:
        return "<span class='glyphicon glyphicon-time' title='Rerun Requested' ></span>";//Archived
      default:
        return "<span class='glyphicon glyphicon-pencil' title='New' ></span>";//New
    }
  }

}
