import { Component, OnInit, Input, Output, EventEmitter, forwardRef } from '@angular/core';
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
export class TinyMceEditorComponent implements ControlValueAccessor, OnInit {
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

  /** Fires on every editor content change (keystroke, paste, undo/redo) and pushes it to the outer form control. */
  onEditorModelChange(html: string): void {
    this.html = html;
    this.onTouched();
    this.onChange(html);
  }

  writeValue(html: string): void {
    this.html = html == null ? '' : html;
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

  ngOnInit(): void {
    this.tinymceOptions = {
      ...this.tinymceOptions,
      height: this.height || 300
    };
  }

  tinymceOptions: any = {
    base_url: '/tinymce', // Root for resources
    suffix: '.min',       // Suffix to use when loading resources
    height: 300,
    menubar: false,
    statusbar: false,
    plugins: ['advlist', 'autolink', 'lists', 'link', 'image', 'charmap', 'print', 'preview', 'anchor', 'searchreplace', 'visualblocks', 'fullscreen', 'insertdatetime', 'media', 'table', 'paste', 'template'],
    toolbar: 'formatselect | undo redo | bold italic strikethrough forecolor backcolor | alignleft aligncenter alignright alignjustify | bullist numlist outdent indent | link image | removeformat',
    templates: [
      { title: 'Course', description: 'Course Templates', url: './pages/templates/course.html' }
    ]
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
    , branding: false
    , convert_urls: false
  }
}
