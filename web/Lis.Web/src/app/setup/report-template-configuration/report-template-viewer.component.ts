import { Component, Input, OnChanges, SimpleChanges, ViewEncapsulation } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

/**
 * Reusable Admin report preview viewer for declarative renderer HTML.
 * Not used by production Diagnostic/Radiology print components.
 */
@Component({
  selector: 'app-report-template-viewer',
  templateUrl: './report-template-viewer.component.html',
  styles: [`
    .rte-viewer { border: 1px solid #ddd; background: #e9e9e9; padding: 12px; margin-top: 12px; }
    .rte-viewer-toolbar { margin-bottom: 10px; min-height: 28px; }
    .rte-viewer-page {
      background: #fff; width: 210mm; min-height: 297mm; margin: 0 auto;
      box-shadow: 0 1px 4px rgba(0,0,0,.2); overflow: hidden;
    }
  `],
  encapsulation: ViewEncapsulation.None
})
export class ReportTemplateViewerComponent implements OnChanges {
  @Input() html: string;
  @Input() css: string;
  @Input() zoom = 1;
  @Input() message: string;
  @Input() pageSize = 'A4';
  @Input() orientation = 'Portrait';

  safeContent: SafeHtml;

  constructor(private sanitizer: DomSanitizer) { }

  ngOnChanges(changes: SimpleChanges): void {
    const styleBlock = this.css ? `<style type="text/css">${this.css}</style>` : '';
    this.safeContent = this.sanitizer.bypassSecurityTrustHtml((styleBlock + (this.html || '')));
  }

  zoomIn() {
    this.zoom = Math.min(2, +(this.zoom + 0.1).toFixed(2));
  }

  zoomOut() {
    this.zoom = Math.max(0.5, +(this.zoom - 0.1).toFixed(2));
  }
}
