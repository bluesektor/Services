DECLARE @MEASURE			 Real = 3956.55; -- miles
DECLARE @clientLat			 Real = 33.369998931884766; -- Decimal(9,6) 
DECLARE @clientLon			 Real = -112.37999725341797; --(9,6) = 
DECLARE @private		bit = 0; -- include private = 1
DECLARE @deleted		bit = 0; -- include deleted = 1
DECLARE @PageIndex  int = 1;
DECLARE @PageSize   int = 50;
DECLARE @parentUUID varchar(32) = '';
DECLARE @endDate Datetime = '2020-1-1 16:00:00'; --GETDATE();
DECLARE @orderBy varchar(32) =  'StartDate'; --'Distance';
DECLARE	@orderDirection varchar(32) = 'ASC';

--SELECT   CalcDistance(@clientLat, @clientLon , e.Latitude, e.Longitude, @MEASURE ) as Distance
--	     ,a.Name AS HostName
--		,e.[UUID]	
--		,e.[Name]	,e.[Category]	,e.[EventDateTime]	,e.[RepeatCount]
--		,e.[RepeatForever]	,e.[Frequency]		,e.[StartDate]	,e.[EndDate]
--		,[Url]				,[HostAccountUUID]	,e.[GUUID]		,e.[GuuidType]
--		,e.[UUIDType]			,e.[UUParentID]   ,e.[UUParentIDType]
--		,e.[Status]			,e.[AccountUUID]      ,e.[Active]		,e.[Deleted]
--		,e.[Private]			,e.[SortOrder]		,e.[CreatedBy]    ,e.[DateCreated]
--		,e.[Image]			,e.[RoleWeight]		,e.[RoleOperation],e.[NSFW]
--		,e.[Latitude]			,e.[Longitude] 		,e.[Description]
--FROM Events e
--JOIN Accounts a ON e.HostAccountUUID = a.UUID
--WHERE 
--	(e.UUID = @parentUUID OR e.UUParentID =  @parentUUID ) AND
--	(e.Private = 0 OR e.Private = @private) AND
--	(e.Deleted = 0 OR e.Deleted = @deleted) AND
--	(e.EndDate > @endDate)
--ORDER BY   Distance asc
--OFFSET @PageSize * (@PageIndex - 1) ROWS
--FETCH NEXT @PageSize ROWS ONLY


----select * from profiles
----select * from events


--select e.UUID, e.Latitude, e.Longitude, e.Name,
--	l.Latitude, l.Longitude, l.UUID, l.EventUUID, l.AccountUUID
--	from events e
--Join EventLocations l on e.UUID = l.EventUUID
--where e.Latitude is null
--order by e.name

-- --select * from EventLocations where accountuuid <>  'c66185a39e1c475a93ba5b053ae31d23'
-- --select * from accounts where uuid = 'c66185a39e1c475a93ba5b053ae31d23'
-- --select * from accounts where name like '%hedo%'
-- -- , 

-- UPDATE Accounts SET Latitude = 18.339004, Longitude = -78.340669
-- where GUUID = 'b8538246bcb24b8abe1c088d3f52ae27'



--SELECT CEILING(dbo.CalcDistance(@CLIENTLAT, @CLIENTLON , e.Latitude, e.Longitude, @MEASURE ) ) as Distance
--                                            ,a.Name AS HostName
--											,el.Name AS Location
--											,el.City
--											,el.State
--											,el.Country
--                                            ,e.[Name]	            ,e.[Category]	        ,e.[EventDateTime],e.[RepeatCount]
--		                                    ,e.[RepeatForever]	,e.[Frequency]		,e.[StartDate]	,e.[EndDate]
--		                                    ,e.[Url]				,e.[HostAccountUUID]	,e.[GUUID]		,e.[GuuidType]
--		                                    ,e.[UUID]				,e.[UUIDType]			,e.[UUParentID]   ,e.[UUParentIDType]
--		                                    ,e.[Status]			,e.[AccountUUID]      ,e.[Active]		,e.[Deleted]
--		                                    ,e.[Private]			,e.[SortOrder]		,e.[CreatedBy]    ,e.[DateCreated]
--		                                    ,e.[Image]			,e.[RoleWeight]		,e.[RoleOperation],e.[NSFW]
--		                                    ,e.[Latitude]			,e.[Longitude] 		,e.[Description], e.[IsAffiliate]
--                                    FROM Events e
--                                    JOIN Accounts a ON e.HostAccountUUID = a.UUID
--									JOIN EventLocations el ON e.UUID = el.EventUUID
--                                   WHERE
--	                                (e.UUID = @PARENTUUID OR e.UUParentID =  @PARENTUUID ) AND
--	                                (e.Private = 0 OR e.Private = @PRIVATE) AND
--	                                (e.Deleted = 0 OR e.Deleted = @DELETED) AND
--	                                (e.EndDate > @ENDDATE) ORDER BY startdate ASC OFFSET @PAGESIZE *(@PAGEINDEX - 1) ROWS FETCH NEXT @PAGESIZE ROWS ONLY

SELECT CEILING(dbo.CalcDistance(@CLIENTLAT, @CLIENTLON , e.Latitude, e.Longitude, @MEASURE ) ) as Distance
                                         ,a.Name AS HostName
                                            ,el.Name AS Location
											,el.City
											,el.State
										,el.Country
                                            ,e.[Name]	 ,e.[UUID]	           ,e.[Category]	        ,e.[EventDateTime],e.[RepeatCount]
		                                    ,e.[RepeatForever]	,e.[Frequency]		,e.[StartDate]	,e.[EndDate]
		                                    ,e.[Url]				,e.[HostAccountUUID]	,e.[GUUID]		,e.[GuuidType]
		                                    			,e.[UUIDType]			,e.[UUParentID]   ,e.[UUParentIDType]
		                                    ,e.[Status]			,e.[AccountUUID]      ,e.[Active]		,e.[Deleted]
		                                    ,e.[Private]			,e.[SortOrder]		,e.[CreatedBy]    ,e.[DateCreated]
		                                    ,e.[Image]			,e.[RoleWeight]		,e.[RoleOperation],e.[NSFW]
		                                    ,e.[Latitude]			,e.[Longitude] 		,e.[Description], e.[IsAffiliate]
											 ,el.Latitude, el.Longitude
											
                                    FROM Events e
                                    LEFT JOIN Accounts a ON e.HostAccountUUID = a.UUID
									    LEFT JOIN (SELECT DISTINCT EventUUID,Latitude, Longitude, Name, City, State, Country FROM EventLocations) el   ON e.UUID = el.EventUUID
                                   WHERE
	                                (e.UUID = @PARENTUUID OR e.UUParentID =  @PARENTUUID ) AND
	                                (e.Private = 0 OR e.Private = @PRIVATE) AND
	                                (e.Deleted = 0 OR e.Deleted = @DELETED) AND
	                                (e.EndDate > @ENDDATE) 
									ORDER BY startdate ASC OFFSET @PAGESIZE *(@PAGEINDEX - 1) ROWS FETCH NEXT @PAGESIZE ROWS ONLY
			
			
			select * from Accounts   where name like '%angel%'						