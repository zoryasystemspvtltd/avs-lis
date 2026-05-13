import { Component, OnInit, Input, Output, EventEmitter, forwardRef } from '@angular/core';
import { environment } from '../../../environments/environment';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
  selector: 'app-tiny-mce-editor',
  templateUrl: './tiny-mce-editor.component.html',
  styleUrls: ['./tiny-mce-editor.component.css'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => TinyMceEditorComponent),
      multi: true
    }
  ]
})
export class TinyMceEditorComponent implements ControlValueAccessor {
  public html: string;
  @Input() disabled = false;
  @Input() height = 300;
  // Function to call when the html changes.
  onChange = (html: string) => { };

  // Function to call when the input is touched
  onTouched = () => { };

  get value(): string {
    return this.html;
  }

  changeHtml($event: any) {
    this.writeValue(this.html);
  }

  writeValue(html: string): void {
    if (html !== undefined) {
      this.html = html;
      this.onTouched();
      this.onChange(this.html);
    }
  }

  registerOnChange(fn: (html: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  tinymceOptions = {
    base_url: '/tinymce', // Root for resources
    suffix: '.min',       // Suffix to use when loading resources
    height: this.height,
    menubar: false,
    statusbar: false,
    plugins: ['advlist autolink lists link image charmap print preview anchor searchreplace visualblocks fullscreen insertdatetime media table contextmenu paste template'],
    toolbar: 'formatselect | undo redo | insert | styleselect | bold italic strikethrough forecolor backcolor | alignleft aligncenter alignright alignjustify | bullist numlist outdent indent | link image  media qimage | template | removeformat',
    templates: [
      //{ title: 'Some title 1', description: 'Some desc 1', content: 'My content' },
      { title: 'Course', description: 'Course Templates', url: './pages/templates/course.html' }
    ]
    //, content_css: [
    //    '//fonts.googleapis.com/css?family=Lato:300,300i,400,400i',
    //    '//www.tinymce.com/css/codepen.min.css',
    //    '//maxcdn.bootstrapcdn.com/bootstrap/3.3.7/css/bootstrap.css'
    //]
    , style_formats: [
      {
        title: 'Headers', items: [
          { title: 'h1', block: 'h1' },
          { title: 'h2', block: 'h2' },
          { title: 'h3', block: 'h3' },
          { title: 'h4', block: 'h4' },
          { title: 'h5', block: 'h5' },
          { title: 'h6', block: 'h6' }
        ]
      },

      {
        title: 'Blocks', items: [
          { title: 'p', block: 'p' },
          { title: 'div', block: 'div' },
          { title: 'pre', block: 'pre' }
        ]
      },

      {
        title: 'Containers', items: [
          { title: 'section', block: 'section', wrapper: true, merge_siblings: false },
          { title: 'article', block: 'article', wrapper: true, merge_siblings: false },
          { title: 'blockquote', block: 'blockquote', wrapper: true },
          { title: 'hgroup', block: 'hgroup', wrapper: true },
          { title: 'aside', block: 'aside', wrapper: true },
          { title: 'figure', block: 'figure', wrapper: true }
        ]
      }
    ]
    , schema: "html5"
    , visualblocks_default_state: true
    , end_container_on_empty_block: true
    , setup: (editor) => {
      editor.ui.registry.addButton('qimage', {
        text: '',
        title: 'Image Galary',
        icon: 'qimage',
        onAction: function () {
          //alert('Custome Button');

          editor.windowManager.open({
            title: 'Select Media Files', // The dialog's title - displayed in the dialog header

            width: 600,
            height: 400,
            body: {
              type: 'panel', // The root body type - a Panel or TabPanel
              items: [ // A list of panel components
                {
                  type: 'htmlpanel', // A HTML panel component
                  html: `<iframe src="${environment.ApplicationServer}/assets/html/media.html" frameborder="0" style="width:100%;"></iframe>`
                }
              ]
            },
            buttons: [ // A list of footer buttons
              {
                type: 'cancel',
                text: 'Cancel'
              },
              {
                type: 'custom',
                text: 'OK',
                name: 'qmedia'
              }
            ],
            onChange: (dialogApi, details) => {
              
              var data = dialogApi.getData();

            },
            onAction: (dialogApi, details) => {
              if (details.name === 'qmedia') {
                var qmedias = JSON.parse(localStorage.getItem("qmedias"));
                let content = '';
                qmedias.forEach(item => {
                  switch (item.extention) {
                    case '.jpg':
                    case '.jpeg':
                    case '.gif':
                    case '.png':
                      content = content + `<img src="${item.url}" title="${item.name}"> <br />`;
                      break;
                    case '.pdf':
                    case '.doc':
                    case '.docx':
                    case '.ppt':
                    case '.pptx':
                    case '.xls':
                    case '.xlsx':
                      content = content + `<a target="_blank" href="${item.url}" title="${item.name}">${item.name}</a> <br />`;
                      break;
                    case 'http':
                      content = content + `<iframe src="${item.url}" width="560" height="314" allowfullscreen="allowfullscreen"></iframe> <br />`;
                      break;
                    default:
                      content = content + `<a target="_blank" href="${item.url}" title="${item.name}">${item.name}</a> <br />`;
                      break;
                  }

                });

                editor.insertContent(content);
                localStorage.removeItem("qmedias");

                dialogApi.close();
              }
            }
          });

        }
      });
    }

  }
}
