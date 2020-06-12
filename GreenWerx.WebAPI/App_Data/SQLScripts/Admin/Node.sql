USE [Prod_platoscom]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[Posts](
	[GUUID] [nvarchar](max) NULL,			-- GUUID
	[GuuidType] [nvarchar](max) NULL,		-- GuuidType
	[UUID] [nvarchar](64) NULL,				-- UUID
	[UUIDType] [nvarchar](32) NULL,			-- UUIDType
	[UUParentID] [nvarchar](64) NULL,		-- UUParentID
	[UUParentIDType] [nvarchar](32) NULL,	-- UUParentIDType
	[Name] [nvarchar](256) NULL,			-- Name
	[Status] [nvarchar](32) NULL,			-- Status
	[AccountUUID] [nvarchar](32) NULL,		--  AccountUUID
	[Active] [bit] NOT NULL,				-- Active
	[Deleted] [bit] NOT NULL,				-- Deleted
	[Private] [bit] NOT NULL,				-- Private
	[SortOrder] [int] NOT NULL,				-- SortOrder
	[CreatedBy] [nvarchar](32) NULL,		-- CreatedBy
	[DateCreated] [smalldatetime] NOT NULL,	-- DateCreated
	[Image] [nvarchar](max) NULL,			-- Image
	[RoleWeight] [int] NOT NULL,			-- RoleWeight
	[RoleOperation] [nvarchar](max) NULL,	-- RoleOperation
	[NSFW] [int] NULL CONSTRAINT [_TODOADDTYPE_PLURALnAME_NSFW]  DEFAULT ((-1)),	  -- NSFW

 CONSTRAINT [IX_UUID_TODOADDTYPE_PLURALnAME] UNIQUE NONCLUSTERED 
(
	[UUID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO


