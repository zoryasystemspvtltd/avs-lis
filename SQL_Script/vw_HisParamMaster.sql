USE [LISStaging]
GO

/****** Object:  View [dbo].[vw_HisParamMaster]    Script Date: 5/26/2024 11:25:24 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

ALTER view [dbo].[vw_HisParamMaster]
as
select distinct
ISNULL(CODE,LISParamCode) AS CODE
,NAME
,ISNULL(HISCODE,HISParamCode) AS HISCODE
,ISNULL(HISNAME,HISParamDescription) AS HISNAME
from HisParamMaster s
right join (
	select distinct HISParamCode,HISParamDescription,LISParamCode from BeckmanLIS.dbo.HISParameterMaster
) m on s.CODE = m.LISParamCode
where isnull(m.LISParamCode,'') <> ''
GO


