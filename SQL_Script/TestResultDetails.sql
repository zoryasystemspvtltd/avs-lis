USE [LISStaging]
GO

/****** Object:  Table [dbo].[TestResultDetails]    Script Date: 5/26/2024 11:33:51 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[TestResultDetails](
	[Id] [bigint] NOT NULL,
	[LISParamCode] [varchar](100) NULL,
	[LISParamValue] [varchar](100) NULL,
	[LISParamUnit] [varchar](200) NULL,
	[CreatedBy] [varchar](100) NULL,
	[CreatedOn] [datetime] NOT NULL,
	[TestResultId] [bigint] NOT NULL,
 CONSTRAINT [PK_TestResultDetails] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO


