/****** Script for SelectTopNRows command from SSMS  ******/
--SELECT TOP 1000
--      [Name]      ,[Category]  , [UUParentIDType]    ,[EventDateTime]      ,[GUUID]      ,[UUID]      ,[UUIDType]
--FROM [Platos_Local].[dbo].[Events]
--where (category = 'resort' or category = 'Resort')
	
--order by GUUID
  -- 3facb51c648c409eae5bd577df5029d6 Hedonism II Kasidie Crush Week
   -- delete en uuid = 3facb51c648c409eae5bd577df5029d6
   --  should show esp version in query

-- 258 resort entries
-- 79  each
-- 
--select * from [Platos_Local].[dbo].[Events] where guuid = '3facb51c648c409eae5bd577df5029d6'
-- delete from [Platos_Local].[dbo].[Events] where uuid = '3facb51c648c409eae5bd577df5029d6'
-- deleted Kasidie Crush Week  SHOULD show spanish entry if spanish is secondary lang Kasidie Crush Week - ESP

DECLARE @MEASURE			 Real = 3956.55; -- miles
DECLARE @clientLat			 Real = 33.369998931884766; -- Decimal(9,6) 
DECLARE @clientLon			 Real = -112.37999725341797; --(9,6) = 
DECLARE @private		bit = 0; -- include private = 1
DECLARE @deleted		bit = 0; -- include deleted = 1
DECLARE @PageIndex  int = 1;
DECLARE @PageSize   int = 500;
DECLARE @parentUUID varchar(32) = '';
DECLARE @endDate Datetime = '2019-1-1 16:00:00'; --GETDATE();
DECLARE @orderBy varchar(32) =  'StartDate'; --'Distance';
DECLARE	@orderDirection varchar(32) = 'ASC';

 DECLARE	 @primaryLanguage	varchar(32) = 'esp';
 DECLARE	 @secondaryLanguage varchar(32) = 'en';
 DECLARE	 @thirdLanguage		varchar(32) = 'kn';

--SELECT CEILING(dbo.CalcDistance(@CLIENTLAT, @CLIENTLON , e.Latitude, e.Longitude, @MEASURE ) ) as Distance
--                                            ,a.Name AS HostName
--                                            ,e.[Name],e.[Category]	 , e.UUParentIDType, e.GUUID
--											,e.[EventDateTime],e.[RepeatCount]
--		                                    ,e.[RepeatForever]	,e.[Frequency]		,e.[StartDate]	,e.[EndDate]
--		                                    ,e.[Url]				,e.[HostAccountUUID]	,e.[GUUID]		,e.[GuuidType]
--		                                    ,e.[UUID]				,e.[UUIDType]			,e.[UUParentID]   ,e.[UUParentIDType]
--		                                    ,e.[Status]			,e.[AccountUUID]      ,e.[Active]		,e.[Deleted]
--		                                    ,e.[Private]			,e.[SortOrder]		,e.[CreatedBy]    ,e.[DateCreated]
--		                                    ,e.[Image]			,e.[RoleWeight]		,e.[RoleOperation],e.[NSFW]
--		                                    ,e.[Latitude]			,e.[Longitude] 		,e.[Description], e.[IsAffiliate]
--                                    FROM Events e
--                                    JOIN Accounts a ON e.HostAccountUUID = a.UUID
--                                   WHERE
--	                                (e.UUID = @PARENTUUID OR e.UUParentID =  @PARENTUUID ) AND
--	                                (e.Private = 0 OR e.Private = @PRIVATE) AND
--	                                (e.Deleted = 0 OR e.Deleted = @DELETED) AND
--	                                (e.EndDate > @ENDDATE) ORDER BY startdate ASC OFFSET @PAGESIZE *(@PAGEINDEX - 1) ROWS FETCH NEXT @PAGESIZE ROWS ONLY



--SELECT     e.[Name],e.[Category]	 , e.UUParentIDType, e.GUUID ,e.[EventDateTime],e.[RepeatCount],e.[StartDate]	,e.[GUUID]  ,e.[UUID],e.[UUParentID]
--          FROM Events e
--        JOIN Accounts a ON e.HostAccountUUID = a.UUID
--        WHERE
--		(e.UUParentIDType = 'esp' OR e.UUParentIDType = 'en') AND
--	    (e.UUID = @PARENTUUID OR e.UUParentID =  @PARENTUUID ) AND
--	    (e.Private = 0 OR e.Private = @PRIVATE) AND
--	    (e.Deleted = 0 OR e.Deleted = @DELETED) AND
--	    (e.EndDate > @ENDDATE) ORDER BY startdate ASC OFFSET @PAGESIZE *(@PAGEINDEX - 1) ROWS FETCH NEXT @PAGESIZE ROWS ONLY

-- PARTID = GUUID
-- UUID = UUID
-- Type = UUParentIDType
-- SettingName = Name

-- Include all selected types, filter out duplicate where primaryType takes precedence.
-- DECLARE	 @primaryType varchar(32) = 'diamond';
-- DECLARE	 @secondType varchar(32)  = 'gold';
-- DECLARE	 @thirdType varchar(32)   = 'nickel';

-- Table
-- SettingName		Type		PARTID  UUID
-- Alpha			diamond		123		123		<== Returned because the primaryType takes precedence 
-- Alpha - g		gold		123		321		<== Not returned because it would be a duplicate, primaryType wins
-- Charlie			diamond		456     456		<== Returned; no duplicate partid and is in type list.
-- Delta - c		copper		789		789		<== Not returned becuase it's not in type list
-- Echo - g			gold		987     987     <== Returned because no duplicate partid and is in type list



 --Alpha			diamond		123		123		<== Returned because the primaryType takes precedence 
 --Charlie			diamond		456     456		<== Returned; no duplicate partid and is in type list.
 --Echo - g			gold		987     987     <== Returned because no duplicate partid and is in type list


-- SELECT * from
--	( SELECT SettingName, Type, PARTID, UUID
--			,row_number() over (partition by PARTID order by Type ) idx
--	  FROM Settings
--	  WHERE
--		(Type = @primaryType OR Type = @secondType OR Type = @thirdType) 
--	) a where idx = 1 


--What I'm trying to accomplish is select from a table and return all the records 
--where the Type matches one of the parameter types. Remove duplicates with the primaryType
--being the record of choice if found, if not then a single record from one of the other type arguments.
--The order doesn't matter for the other types although it would be nice (can pick @secondType or @thirdType).
--This is a POC project so if I need to add or change things to make it easier/faster I'm open to ideas!

--```
--NOTE: UUID and PARTID are alphanumeric.	
--DECLARE	 @primaryType varchar(32) = 'diamond';
--DECLARE	 @secondType varchar(32)  = 'gold';
--DECLARE	 @thirdType varchar(32)   = 'nickel';


--SettingName		Type		PARTID  UUID
------------------------------------------
--Alpha			diamond		123		123		<== Returned because the primaryType takes precedence 
--Alpha - g		gold		123		321		<== Not returned because it would be a duplicate, primaryType wins
--Charlie			diamond		456     456		<== Returned; no duplicate partid and is in type list.
--Delta - c		copper		789		789		<== Not returned becuase it's not in type list
--Echo - g		gold		987     987     <== Returned because no duplicate partid and is in type list
--```
--Desired result...
--```
--Alpha			diamond		123		123		<== Returned because the primaryType takes precedence 
--Charlie			diamond		456     456		<== Returned no duplicate partid and is in type list.
--Echo - g		gold		987     987     <== Returned because no duplicate partid and is in type list

--```



--select   e.[Name],e.[Category]	 , e.UUParentIDType, e.GUUID ,e.[EventDateTime],e.[RepeatCount],e.[StartDate]	,e.[GUUID]  ,e.[UUID],e.[UUParentID]
--from Events as e
--where UUParentIDType = 'esp' OR UUParentIDType = 'en'
--INNER JOIN
-- ( 
--   select *
--   from Events as t2
--   where e.GUUID = t2.GUUID
--   and e.UUParentIDType = 'en'OR UUParentIDType = 'en'
         
-- )ORDER BY startdate ASC



-- PARTID = GUUID
-- UUID = UUID
-- Type = UUParentIDType
-- SettingName = Name

-- Hedonism II Kasidie Crush Week
------------------------------------------- this works but the order by in the partition doesn't let us select primary language
SELECT * from
			(	SELECT CEILING(dbo.CalcDistance(@CLIENTLAT, @CLIENTLON , e.Latitude, e.Longitude, @MEASURE ) ) as Distance
                                            ,a.Name AS HostName
                                            ,e.[Name],e.[Category]	 , e.UUParentIDType, e.GUUID
											,e.[EventDateTime],e.[RepeatCount]
		                                    ,e.[RepeatForever]	,e.[Frequency]		,e.[StartDate]	,e.[EndDate]
		                                    ,e.[Url]				,e.[HostAccountUUID]			,e.[GuuidType]
		                                    ,e.[UUID]				,e.[UUIDType]			,e.[UUParentID]   
		                                    ,e.[Status]			,e.[AccountUUID]      ,e.[Active]		,e.[Deleted]
		                                    ,e.[Private]			,e.[SortOrder]		,e.[CreatedBy]    ,e.[DateCreated]
		                                    ,e.[Image]			,e.[RoleWeight]		,e.[RoleOperation],e.[NSFW]
		                                    ,e.[Latitude]			,e.[Longitude] 		--,e.[Description], e.[IsAffiliate]
											---- ,row_number() over (partition by e.GUUID order by e.UUParentIDType desc) idx -- this will mix becuase esp will be sorted before
											--,row_number() over (partition by e.GUUID order by e.UUParentIDType) idx
									,row_number() over(partition by e.GUUID order by case e.UUParentIDType when  @primaryLanguage then 1 when @secondaryLanguage then 2 when @thirdLanguage then 3 else 4 end, len(e.UUParentIDType), e.[UUID] /*id as final tie-breaker*/) as rownum
                                    FROM Events e
                                    JOIN Accounts a ON e.HostAccountUUID = a.UUID
                                   WHERE
									(e.UUParentIDType = 'esp' OR e.UUParentIDType = 'en') and
	                                (e.UUID = @PARENTUUID OR e.UUParentID =  @PARENTUUID ) AND
	                                (e.Private = 0 OR e.Private = @PRIVATE) AND
	                                (e.Deleted = 0 OR e.Deleted = @DELETED) AND
	                                (e.EndDate > @ENDDATE) 
			 ) a where rownum = 1 ORDER BY startdate ASC OFFSET @PAGESIZE *(@PAGEINDEX - 1) ROWS FETCH NEXT @PAGESIZE ROWS ONLY
 
 	
  
 		


--select * 
--from
--(
--    select *, 
--        row_number() over(partition by PARTID order by case Type when  @primaryType then 1 when @secondType then 2 when @thirdType then 3 else 4 end, len(Type), id /*id as final tie-breaker*/) as rownum
--    from @t
--    where Type in (@primaryType, @secondType, @thirdType)
--) as a
----where a.rownum = 1 