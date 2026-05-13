USE [LISStaging]
GO

/****** Object:  View [dbo].[vwTestMaster]    Script Date: 5/26/2024 11:26:29 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


ALTER VIEW [dbo].[vwTestMaster]
AS
SELECT [TESTID] AS TestId  
      ,[TCODE]  AS TestAlias
      ,[TESTNM]   AS TestName
      ,[SAMPLEID]  AS SampleId 
      ,[SAMPLENM]  AS [Sample]
      ,[ACTIVE] AS Active
  FROM [NEOSOFT]..[LIS_ZO].[LIS_TESTMAST]
  WHERE ACTIVE = 1



GO


