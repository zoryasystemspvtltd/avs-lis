-- Returns the QA certification seed manifest (fixed keys for automation).
SET NOCOUNT ON;

SELECT EntityType, EntityKey, EntityId, Detail
FROM (
    SELECT N'LaboratoryPatient' AS EntityType, N'QA-CERT-LAB-PAT' AS EntityKey, CAST(Id AS NVARCHAR(30)) AS EntityId, Name AS Detail FROM PatientDetails WHERE HisPatientId = N'QA-CERT-LAB-PAT'
    UNION ALL SELECT N'RadiologyPatient', N'QA-CERT-RAD-PAT', CAST(Id AS NVARCHAR(30)), Name FROM PatientDetails WHERE HisPatientId = N'QA-CERT-RAD-PAT'
    UNION ALL SELECT N'TestProfile', N'QA-CERT-PROF', CAST(Id AS NVARCHAR(30)), Name FROM TestProfileMaster WHERE Code = N'QA-CERT-PROF'
    UNION ALL SELECT N'AnalyzerTest', N'QA-CERT-ANLZ', CAST(Id AS NVARCHAR(30)), HISTestCodeDescription FROM HISTestMaster WHERE HISTestCode = N'QA-CERT-ANLZ'
    UNION ALL SELECT N'ManualTest', N'QA-CERT-MAN', CAST(Id AS NVARCHAR(30)), HISTestCodeDescription FROM HISTestMaster WHERE HISTestCode = N'QA-CERT-MAN'
    UNION ALL SELECT N'RadiologyTest', N'QA-CERT-MRI', CAST(Id AS NVARCHAR(30)), HISTestCodeDescription FROM HISTestMaster WHERE HISTestCode = N'QA-CERT-MRI'
    UNION ALL SELECT N'PendingSample', N'QA-CERT-SMP-PEND', CAST(Id AS NVARCHAR(30)), HISTestCode FROM TestRequestDetails WHERE SampleNo = N'QA-CERT-SMP-PEND'
    UNION ALL SELECT N'CollectedSample', N'QA-CERT-SMP-COLL', CAST(Id AS NVARCHAR(30)), HISTestCode FROM TestRequestDetails WHERE SampleNo = N'QA-CERT-SMP-COLL'
    UNION ALL SELECT N'ReceivedSample', N'QA-CERT-SMP-RECV', CAST(Id AS NVARCHAR(30)), HISTestCode FROM TestRequestDetails WHERE SampleNo = N'QA-CERT-SMP-RECV'
    UNION ALL SELECT N'DoctorUser', N'qa-cert-doctor@zorya.co.in', Id, N'QA Cert Doctor' FROM AspNetUsers WHERE UserName = N'qa-cert-doctor@zorya.co.in'
    UNION ALL SELECT N'TechnicianUser', N'qa-cert-tech@zorya.co.in', Id, N'Technician' FROM AspNetUsers WHERE UserName = N'qa-cert-tech@zorya.co.in'
    UNION ALL SELECT N'AnalyzerEquipment', N'QA-CERT-EQ-KEY', CAST(Id AS NVARCHAR(30)), Name FROM EquipmentMaster WHERE AccessKey = N'QA-CERT-EQ-KEY'
    UNION ALL SELECT N'RadiologyRequest', N'QA-CERT-INV-RAD', CAST(Id AS NVARCHAR(30)), HISTestCode FROM RadiologyRequestDetail WHERE HISRequestNo = N'QA-CERT-INV-RAD'
) m
ORDER BY EntityType;
