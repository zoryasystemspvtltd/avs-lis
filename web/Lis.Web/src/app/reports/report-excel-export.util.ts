/** Lightweight Excel export via SpreadsheetML (opens in Excel, no extra npm packages). */

export interface ExcelColumn<T> {
  header: string;
  width?: number;
  type?: 'text' | 'date' | 'currency';
  value: (row: T) => string | number | null | undefined;
}

function escapeXml(value: string): string {
  if (value == null) {
    return '';
  }
  return String(value)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

function formatDateForExcel(value: string | Date): string {
  if (!value) {
    return '';
  }
  const d = value instanceof Date ? value : new Date(value);
  if (isNaN(d.getTime())) {
    return String(value);
  }
  const day = d.getDate().toString().padStart(2, '0');
  const month = (d.getMonth() + 1).toString().padStart(2, '0');
  return `${day}/${month}/${d.getFullYear()}`;
}

function cellXml(value: string | number, styleId: string, dataType: 'String' | 'Number'): string {
  const v = value == null ? '' : value;
  return `<Cell ss:StyleID="${styleId}"><Data ss:Type="${dataType}">${escapeXml(String(v))}</Data></Cell>`;
}

export function exportRowsToExcel<T>(
  sheetName: string,
  fileName: string,
  columns: ExcelColumn<T>[],
  rows: T[]
): void {
  const headerCells = columns.map(c =>
    cellXml(c.header, 'Header', 'String')
  ).join('');

  const bodyRows = rows.map(row => {
    const cells = columns.map(col => {
      const raw = col.value(row);
      if (col.type === 'currency') {
        const num = raw == null || raw === '' ? 0 : Number(raw);
        return cellXml(isNaN(num) ? 0 : num.toFixed(2), 'Currency', 'Number');
      }
      if (col.type === 'date') {
        return cellXml(formatDateForExcel(raw as string), 'DateCell', 'String');
      }
      return cellXml(raw == null ? '' : String(raw), 'Text', 'String');
    }).join('');
    return `<Row>${cells}</Row>`;
  }).join('');

  const colWidths = columns.map(c =>
    `<Column ss:Width="${(c.width || 90) * 0.75}"/>`
  ).join('');

  const xml = `<?xml version="1.0" encoding="UTF-8"?>
<?mso-application progid="Excel.Sheet"?>
<Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet"
 xmlns:o="urn:schemas-microsoft-com:office:office"
 xmlns:x="urn:schemas-microsoft-com:office:excel"
 xmlns:ss="urn:schemas-microsoft-com:office:spreadsheet">
<Styles>
  <Style ss:ID="Header">
    <Font ss:Bold="1" ss:Size="11"/>
    <Interior ss:Color="#E8F4F6" ss:Pattern="Solid"/>
    <Alignment ss:Horizontal="Center" ss:Vertical="Center"/>
  </Style>
  <Style ss:ID="Text"><Alignment ss:Vertical="Center"/></Style>
  <Style ss:ID="DateCell"><Alignment ss:Horizontal="Center"/></Style>
  <Style ss:ID="Currency">
    <NumberFormat ss:Format="0.00"/>
    <Alignment ss:Horizontal="Right"/>
  </Style>
</Styles>
<Worksheet ss:Name="${escapeXml(sheetName.substring(0, 31))}">
  <Table>
    ${colWidths}
    <Row>${headerCells}</Row>
    ${bodyRows}
  </Table>
</Worksheet>
</Workbook>`;

  const blob = new Blob([xml], { type: 'application/vnd.ms-excel;charset=utf-8;' });
  const link = document.createElement('a');
  const url = URL.createObjectURL(blob);
  link.href = url;
  link.download = fileName.endsWith('.xls') ? fileName : `${fileName}.xls`;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}

export function reportFileName(prefix: string): string {
  const d = new Date();
  const y = d.getFullYear();
  const m = (d.getMonth() + 1).toString().padStart(2, '0');
  const day = d.getDate().toString().padStart(2, '0');
  return `${prefix}_${y}${m}${day}.xls`;
}

export function displayValue(value: string | number | null | undefined, fallback = '-'): string {
  if (value == null || value === '') {
    return fallback === '-' ? '' : fallback;
  }
  return String(value);
}
