-- Audit: profiles containing tests from Diagnostic departments
SELECT
    p.Id AS ProfileId,
    p.Code AS ProfileCode,
    p.Name AS ProfileName,
    t.Id AS TestId,
    t.HISTestCode AS TestCode,
    t.HISTestCodeDescription AS TestName,
    t.DepartmentCode,
    d.ProcessingCategory
FROM TestProfileMaster p
INNER JOIN TestProfileDetail pd ON pd.TestProfileId = p.Id
INNER JOIN HISTestMaster t ON t.Id = pd.TestId
INNER JOIN Department d ON d.Code = t.DepartmentCode
WHERE d.ProcessingCategory = N'Diagnostic'
ORDER BY p.Code, t.HISTestCode;
