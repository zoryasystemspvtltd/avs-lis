USE [LISStaging]
GO

/****** Object:  Table [dbo].[HisParamMaster]    Script Date: 5/26/2024 11:29:34 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[HisParamMaster](
	[CODE] [varchar](8) NOT NULL,
	[NAME] [varchar](100) NOT NULL,
	[HISCODE] [varchar](50) NULL,
	[HISNAME] [varchar](max) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO


