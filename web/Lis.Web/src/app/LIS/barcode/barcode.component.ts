import { Input } from '@angular/core';
import { Component, OnInit } from '@angular/core';
import { SafeHtml, DomSanitizer } from '@angular/platform-browser';
@Component({
  selector: 'app-barcode',
  templateUrl: './barcode.component.html',
  styleUrls: ['./barcode.component.css']
})
export class BarcodeComponent implements OnInit {
  @Input() code: string;
  @Input() text: string;
  trustedHtml: any;
  currentDate: Date;
  codeLower:string;
  bedNo:string;
  ipNo:string;
  mrNo:string;

  constructor(private sanitizer: DomSanitizer) { }
  groupName: string;
  ngOnInit(): void {
    this.currentDate = new Date();
    let vals = this.text.split('#');
    this.trustedHtml = this.sanitizer.bypassSecurityTrustHtml(vals[0]);
    this.bedNo= vals[1];
    this.ipNo= vals[2];
    this.mrNo= vals[3];
    this.getGroupName();
    this.codeLower = this.code.toLowerCase();
  }

  getGroupName() {
    let groupTag = this.code.charAt(this.code.length - 1);
    switch (groupTag) {
      case 'c':
      case 'C':
        this.groupName = 'CITR.PLAS'
        break;

      case 'e':
      case 'E':
        this.groupName = 'EDTA'
        break;

      case 'f':
      case 'F':
        this.groupName = 'F'
        break;

      case 'g':
      case 'G':
        this.groupName = 'PLAIN'
        break;

      case 'p':
      case 'P':
        this.groupName = 'PP'
        break;

      case 'r':
      case 'R':
        this.groupName = 'RAN'
        break;

      case 'u':
      case 'U':
        this.groupName = 'URINE'
        break;
      default:
        this.groupName = '';
        break;
    }
  }
}
