USE [LISStaging]
GO

/****** Object:  View [dbo].[vwTestReq]    Script Date: 5/26/2024 11:27:07 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



ALTER VIEW [dbo].[vwTestReq]
AS
SELECT [TYP]     
      ,[CANCELLED_HDR]    
      ,[CANCELLED_DTL]    
      ,[IPNO]    
      ,[BEDNO]     
      ,[MRNO]   
      ,[REQID]    
      ,[REQNO]  
      ,[DEPTNM] 
      ,[TESTID]
      ,[GROUPID]    
      ,[GROUPNM]  
      ,[DEPTID]    
      ,[TESTNM]     
      ,[PATIENTNM]
      ,[AGE]
      ,[YMD]     
      ,[SX]    
      ,[REQDTTM]   
      ,[RCDATE]      
      ,[SADATE]
      ,[COLDATE]     
      ,[COLLTIME]
      ,[COLLDTTM]     
      ,[PRINTDT]    
      ,[PRINTTM]     
      ,[PRINTDTTM]     
      ,[APPROVEDDT]     
      ,[APPROVEDTM]    
      ,[APPROVEDTTM]
      ,[PERFORMEDDT]     
      ,[PERFORMEDTM]    
      ,[PERFORMDTTM]     
      ,[DRNAME]    
      ,[IPOPDOCNM]
	  ,[EDCOUNT]
  FROM [NEOSOFT]..[LIS_ZO].[LIS_TESTREQ]
  WHERE CAST(REQDTTM AS DATE) >= CAST(GETDATE() AS DATE)
  AND [DEPTNM] NOT IN (
	'RADIOLOGY'
	,'C.T.SCAN'
	,'SONOGRAPHY'
	,'MAMOGRAPHY'
	,'M.R.I'
	,'CARDIOLOGY'
	,'EYE CLINIC'
	,'GEN. PHYS. CON.(PKG.)'
	,'GYANECOLOGIST CONSULT'
	,'IMMUNO - HISTOCHEMIST'
	,'NEUROLOGY'
)




GO


