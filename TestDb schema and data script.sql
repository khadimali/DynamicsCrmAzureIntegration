USE [TestDb]
GO
/****** Object:  Table [dbo].[Student]    Script Date: 6/11/2016 3:09:59 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Student](
	[StudentId] [int] NOT NULL,
	[Name] [varchar](100) NULL,
	[PhoneNumber] [varchar](100) NULL,
 CONSTRAINT [PK_Student] PRIMARY KEY CLUSTERED 
(
	[StudentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
INSERT [dbo].[Student] ([StudentId], [Name], [PhoneNumber]) VALUES (1, N'Adam', N'+92 300 1234567')
INSERT [dbo].[Student] ([StudentId], [Name], [PhoneNumber]) VALUES (2, N'John', N'+92 301 7654321')
INSERT [dbo].[Student] ([StudentId], [Name], [PhoneNumber]) VALUES (3, N'Jacob', N'+92 302 1112223')
