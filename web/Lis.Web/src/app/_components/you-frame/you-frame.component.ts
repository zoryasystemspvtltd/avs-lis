import { Component, OnInit ,Input} from '@angular/core';
import {SafeHtml,DomSanitizer} from '@angular/platform-browser';

@Component({
  selector: 'app-you-frame',
  templateUrl: './you-frame.component.html',
  styleUrls: ['./you-frame.component.css']
})
export class YouFrameComponent implements OnInit {

  @Input() html:string;
  @Input() maxLength: number;
  @Input() htmlToText:boolean=false;

  public trustedHtml:any;
  constructor(private sanitizer: DomSanitizer) {
    
  }

  ngOnInit() {
    if(this.htmlToText){
      this.html = this.transform(this.html);
    }
    
    if(this.maxLength > 0){
      this.html = this.transform(this.html);
      this.html = this.html.substring(0,this.maxLength); 
    }

    this.trustedHtml = this.sanitizer.bypassSecurityTrustHtml(this.html);
    
  }

  transform(value: string): string {
    return value ? value.replace(/<\/?[^>]+>/ig, " ") : '';
  }

}
