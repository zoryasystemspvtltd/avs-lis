USE [LISStaging]
GO

/****** Object:  Table [dbo].[TestResults]    Script Date: 5/26/2024 11:34:23 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[TestResults](
	[Id] [bigint] NOT NULL,
	[SampleNo] [varchar](100) NULL,
	[HISTestCode] [varchar](100) NULL,
	[LISTestCode] [varchar](100) NULL,
	[SpecimenCode] [varchar](100) NULL,
	[SpecimenName] [varchar](255) NULL,
	[ResultDate] [datetime] NOT NULL,
	[SampleCollectionDate] [datetime] NOT NULL,
	[SampleReceivedDate] [datetime] NOT NULL,
	[AuthorizationDate] [datetime] NULL,
	[AuthorizedBy] [varchar](100) NULL,
	[ReviewDate] [datetime] NULL,
	[ReviewedBy] [varchar](100) NULL,
	[TechnicianNote] [varchar](1000) NULL,
	[DoctorNote] [varchar](1000) NULL,
	[CreatedBy] [varchar](100) NULL,
	[CreatedOn] [datetime] NOT NULL,
	[PatientId] [bigint] NOT NULL,
	[TestRequestId] [bigint] NOT NULL,
	[EquipmentId] [int] NOT NULL,
 CONSTRAINT [PK_TestResults] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO


