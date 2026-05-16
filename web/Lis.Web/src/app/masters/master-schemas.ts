export const MASTER_LIST_SCHEMAS: { [key: string]: any } = {
  department: {
    url: 'departments', heading: 'Department Master', module: 'Department', hideAction: true, hideCreate: false,
    elements: [
      { heading: 'Code', fieldName: 'code', sortable: true, width: '20%', type: 'link_search' },
      { heading: 'Name', fieldName: 'name', sortable: true, width: '80%', type: 'label' }
    ]
  },
  specimen: {
    url: 'specimens', heading: 'Specimen Master', module: 'Specimens', hideAction: true,
    elements: [
      { heading: 'Code', fieldName: 'code', sortable: true, width: '20%', type: 'link' },
      { heading: 'Name', fieldName: 'name', sortable: true, width: '50%', type: 'label' },
      { heading: 'Active', fieldName: 'isActive', sortable: false, width: '15%', type: 'label' }
    ]
  },
  histest: {
    url: 'test-master', heading: 'Test Master', module: 'HisTest', hideAction: true,
    elements: [
      { heading: 'Test Code', fieldName: 'hisTestCode', sortable: true, width: '15%', type: 'link' },
      { heading: 'Test Name', fieldName: 'hisTestCodeDescription', sortable: true, width: '35%', type: 'label' },
      { heading: 'Department', fieldName: 'departmentName', sortable: false, width: '20%', type: 'label' },
      { heading: 'Specimen', fieldName: 'hisSpecimenName', sortable: false, width: '20%', type: 'label' },
      { heading: 'Active', fieldName: 'isActive', sortable: false, width: '10%', type: 'label' }
    ]
  },
  referralDoctor: {
    url: 'referral-doctors', heading: 'Referral Doctor Master', module: 'ReferralDoctor', hideAction: true,
    elements: [
      { heading: 'Code', fieldName: 'code', sortable: true, width: '15%', type: 'link' },
      { heading: 'Name', fieldName: 'name', sortable: true, width: '30%', type: 'label' },
      { heading: 'Phone', fieldName: 'phone', sortable: false, width: '20%', type: 'label' },
      { heading: 'Active', fieldName: 'isActive', sortable: false, width: '10%', type: 'label' }
    ]
  },
  corporate: {
    url: 'corporates', heading: 'Corporate Master', module: 'Corporate', hideAction: true,
    elements: [
      { heading: 'Code', fieldName: 'code', sortable: true, width: '15%', type: 'link' },
      { heading: 'Name', fieldName: 'name', sortable: true, width: '40%', type: 'label' },
      { heading: 'Discount %', fieldName: 'defaultDiscountPercent', sortable: false, width: '15%', type: 'label' },
      { heading: 'Active', fieldName: 'isActive', sortable: false, width: '10%', type: 'label' }
    ]
  },
  testGroup: {
    url: 'test-groups', heading: 'Test Group Master', module: 'TestGroup', hideAction: true,
    elements: [
      { heading: 'Code', fieldName: 'code', sortable: true, width: '20%', type: 'link' },
      { heading: 'Name', fieldName: 'name', sortable: true, width: '40%', type: 'label' },
      { heading: 'Tag', fieldName: 'specimenTag', sortable: false, width: '15%', type: 'label' }
    ]
  },
  testCategory: {
    url: 'test-categories', heading: 'Test Category Master', module: 'TestCategory', hideAction: true,
    elements: [
      { heading: 'Code', fieldName: 'code', sortable: true, width: '20%', type: 'link' },
      { heading: 'Name', fieldName: 'name', sortable: true, width: '60%', type: 'label' }
    ]
  },
  unit: {
    url: 'units', heading: 'Unit Master', module: 'Unit', hideAction: true,
    elements: [
      { heading: 'Code', fieldName: 'code', sortable: true, width: '20%', type: 'link' },
      { heading: 'Name', fieldName: 'name', sortable: true, width: '60%', type: 'label' }
    ]
  },
  method: {
    url: 'methods', heading: 'Method Master', module: 'Method', hideAction: true,
    elements: [
      { heading: 'Code', fieldName: 'code', sortable: true, width: '20%', type: 'link' },
      { heading: 'Name', fieldName: 'name', sortable: true, width: '60%', type: 'label' }
    ]
  },
  sampleType: {
    url: 'sample-types', heading: 'Sample Type Master', module: 'SampleType', hideAction: true,
    elements: [
      { heading: 'Code', fieldName: 'code', sortable: true, width: '20%', type: 'link' },
      { heading: 'Name', fieldName: 'name', sortable: true, width: '60%', type: 'label' }
    ]
  },
  container: {
    url: 'containers', heading: 'Container Master', module: 'Container', hideAction: true,
    elements: [
      { heading: 'Code', fieldName: 'code', sortable: true, width: '15%', type: 'link' },
      { heading: 'Name', fieldName: 'name', sortable: true, width: '40%', type: 'label' },
      { heading: 'Color', fieldName: 'color', sortable: false, width: '15%', type: 'label' }
    ]
  },
  testProfile: {
    url: 'test-profiles', heading: 'Test Profile / Package Master', module: 'TestProfile', hideAction: true,
    elements: [
      { heading: 'Code', fieldName: 'code', sortable: true, width: '20%', type: 'link' },
      { heading: 'Name', fieldName: 'name', sortable: true, width: '40%', type: 'label' },
      { heading: 'Package Rate', fieldName: 'packageRate', sortable: false, width: '20%', type: 'label' }
    ]
  },
  testRate: {
    url: 'test-rates', heading: 'Test Rate Master', module: 'TestRate', hideAction: true,
    elements: [
      { heading: 'Test', fieldName: 'testName', sortable: false, width: '25%', type: 'link' },
      { heading: 'Rate', fieldName: 'rate', sortable: true, width: '12%', type: 'label' },
      { heading: 'Emergency', fieldName: 'emergencyRate', sortable: false, width: '12%', type: 'label' },
      { heading: 'Discount %', fieldName: 'discountPercent', sortable: false, width: '12%', type: 'label' },
      { heading: 'Tax %', fieldName: 'taxPercent', sortable: false, width: '10%', type: 'label' },
      { heading: 'Effective From', fieldName: 'effectiveStart', sortable: true, width: '15%', type: 'date', format: 'dd/MM/yyyy' },
      { heading: 'Active', fieldName: 'isActive', sortable: false, width: '8%', type: 'label' }
    ]
  },
  hisParameter: {
    url: 'his-parameters', heading: 'Parameter Master', module: 'HisParameterMaster', hideAction: true,
    elements: [
      { heading: 'Param Code', fieldName: 'hisParamCode', sortable: true, width: '15%', type: 'link' },
      { heading: 'Description', fieldName: 'hisParamDescription', sortable: true, width: '30%', type: 'label' },
      { heading: 'Test Code', fieldName: 'hisTestCode', sortable: true, width: '15%', type: 'label' },
      { heading: 'Unit', fieldName: 'hisParamUnit', sortable: false, width: '12%', type: 'label' },
      { heading: 'Method', fieldName: 'hisParamMethod', sortable: false, width: '12%', type: 'label' }
    ]
  },
  hisParameterRange: {
    url: 'his-parameter-ranges', heading: 'Parameter Range Master', module: 'HisParameterRangeMaster', hideAction: true,
    elements: [
      { heading: 'Range Code', fieldName: 'hisRangeCode', sortable: true, width: '15%', type: 'link' },
      { heading: 'Range Value', fieldName: 'hisRangeValue', sortable: true, width: '25%', type: 'label' },
      { heading: 'Gender', fieldName: 'gender', sortable: false, width: '10%', type: 'label' },
      { heading: 'Min', fieldName: 'minValue', sortable: false, width: '10%', type: 'label' },
      { heading: 'Max', fieldName: 'maxValue', sortable: false, width: '10%', type: 'label' }
    ]
  },
  testMapping: {
    url: 'test-mappings', heading: 'Test Mapping Master', module: 'TestMappingMaster', hideAction: true,
    elements: [
      { heading: 'Test', fieldName: 'hisTestCode', sortable: true, width: '15%', type: 'link' },
      { heading: 'LIS Test', fieldName: 'lisTestCode', sortable: true, width: '15%', type: 'label' },
      { heading: 'Equipment', fieldName: 'groupName', sortable: false, width: '25%', type: 'label' },
      { heading: 'Active', fieldName: 'isActive', sortable: false, width: '10%', type: 'label' }
    ]
  },
  testParameter: {
    url: 'test-parameters', heading: 'Test Parameter (Sample Lines)', module: 'TestParameterCatalog', hideAction: true, hideCreate: true,
    elements: [
      { heading: 'Param Code', fieldName: 'hisParamCode', sortable: false, width: '20%', type: 'label' },
      { heading: 'Param Name', fieldName: 'hisParamName', sortable: false, width: '30%', type: 'label' },
      { heading: 'Test Code', fieldName: 'hisTestCode', sortable: false, width: '20%', type: 'label' },
      { heading: 'Created', fieldName: 'createdOn', sortable: false, width: '20%', type: 'date', format: 'dd/MM/yyyy' }
    ]
  },
  patientMaster: {
    url: 'patient-master', heading: 'Patient Master', module: 'PatientMaster', hideAction: true,
    elements: [
      { heading: 'Name', fieldName: 'name', sortable: true, width: '25%', type: 'link' },
      { heading: 'Phone', fieldName: 'phone', sortable: false, width: '15%', type: 'label' },
      { heading: 'Gender', fieldName: 'gender', sortable: false, width: '10%', type: 'label' },
      { heading: 'Age', fieldName: 'age', sortable: false, width: '10%', type: 'label' },
      { heading: 'External ID', fieldName: 'hisPatientId', sortable: false, width: '15%', type: 'label' },
      { heading: 'Active', fieldName: 'isActive', sortable: false, width: '10%', type: 'label' }
    ]
  },
  saleInvoice: {
    url: 'sale-invoices', heading: 'Sale Invoices', module: 'SaleInvoice', hideAction: true, hideCreate: false,
    elements: [
      { heading: 'Invoice No', fieldName: 'invoiceNo', sortable: true, width: '18%', type: 'link' },
      { heading: 'Date', fieldName: 'invoiceDate', sortable: true, width: '14%', type: 'date', format: 'dd/MM/yyyy' },
      { heading: 'Patient', fieldName: 'patientName', sortable: false, width: '22%', type: 'label' },
      { heading: 'Net Amount', fieldName: 'netAmount', sortable: true, width: '12%', type: 'currency' },
      { heading: 'Status', fieldName: 'invoiceStatus', sortable: false, width: '12%', type: 'invoice_status' },
      { heading: 'Payment', fieldName: 'paymentStatus', sortable: false, width: '12%', type: 'payment_status' }
    ]
  }
};
