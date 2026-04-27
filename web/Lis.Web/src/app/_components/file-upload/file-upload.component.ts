import { Component, OnInit, Output, EventEmitter, Input } from '@angular/core';
import { HttpClient, HttpRequest, HttpEventType, HttpResponse } from '@angular/common/http';


@Component({
  selector: 'app-fileupload',
  templateUrl: './file-upload.component.html',
  styleUrls: ['./file-upload.component.css']
})
export class FileuploadComponent {
  @Output() onUploaded = new EventEmitter<string>(); 
  @Input() acceptedFile: string;
  public progress: number;
  public fileInfo: any;
  constructor(private http: HttpClient) { }

  upload(files) {
    if (files.length === 0)
      return;

    

    for (let file of files) {
      const formData = new FormData();
      
      formData.append(file.name, file);

      const uploadReq = new HttpRequest('POST', `api/FileUpload`, formData, {
        reportProgress: true,
      });
      this.http.request(uploadReq).subscribe(event => {
        if (event.type === HttpEventType.UploadProgress) {
          this.progress = Math.round(100 * event.loaded / event.total);
        }
        else if (event.type === HttpEventType.Response) {
          this.fileInfo = event.body;
          this.onUploaded.emit(this.fileInfo)
        }
      });
    }
  }
}


