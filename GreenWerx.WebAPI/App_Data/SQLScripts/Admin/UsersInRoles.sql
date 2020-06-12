
				 
 SELECT * FROM RolesBlocked
 SELECT * FROM RolesBlockedUsers
-- delete FROM RolesBlockedUsers

SELECT * FROM UsersInRoles where accountUUID = ' '

SELECT * FROM Roles WHERE  accountUUID = ' ' 
SELECT * FROM UsersInRoles   

INSERT INTO [dbo].[UsersInRoles]
           ([ReferenceUUID],[RoleUUID]          -- ,[Type]
           ,[Action],[AppType]           --,[StartDate]          -- ,[EndDate]
           ,[Persists],[GUUID],[GuuidType],[UUID],[UUIDType]          -- ,[UUParentID]          -- ,[UUParentIDType]          -- ,[Name]          -- ,[Status]
           ,[AccountUUID],[Active],[Deleted],[Private],[SortOrder],[CreatedBy],[DateCreated]          -- ,[Image]
           ,[RoleWeight],[RoleOperation],[NSFW])
     VALUES
           (' ' --user uuid
           ,' ' --role uuid
          -- ,<Type, nvarchar(max),>
           ,'get'
           ,'web'
          -- ,<StartDate, smalldatetime,>
          -- ,<EndDate, smalldatetime,>
           ,1 -- persists
           ,' ' --<GUUID, nvarchar(max),>
           ,'UserRole' --<GuuidType, nvarchar(max),>
           ,' ' --<UUID, nvarchar(64),>
           ,'UserRole' --<UUIDType, nvarchar(32),>
          -- ,<UUParentID, nvarchar(64),>
         --  ,<UUParentIDType, nvarchar(32),>
          -- ,<Name, nvarchar(128),>
         --  ,<Status, nvarchar(32),>
           ,' ' --<AccountUUID, nvarchar(32),>
           ,1 --<Active, bit,>
           ,0--<Deleted, bit,>
           ,1--<Private, bit,>
           ,0--<SortOrder, int,>
           ,' '--<CreatedBy, nvarchar(32),>
           ,'12/24/1971'--<DateCreated, smalldatetime,>
          -- ,<Image, nvarchar(max),>
           ,4--<RoleWeight, int,>
           ,'='--<RoleOperation, nvarchar(max),>
           ,0)--<NSFW, bit,>)
GO
