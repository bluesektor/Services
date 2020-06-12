DECLARE @UUID VARCHAR(32) = '71eb539c184149f39e5d69bd77825a3b';
 
SELECT p.[Name],p.[LocationUUID],p.[UUID] 
		--,l.[Name] as locationName
		/*,p.[LocationType] ,p.[Theme],p.[View],p.[UserUUID],p.[GUUID],p.[GuuidType] 
		,p.[UUIDType] ,p.[UUParentID],p.[UUParentIDType],p.[Status],p.[AccountUUID],p.[Active]
		,p.[Deleted],p.[Private],p.[SortOrder],p.[CreatedBy],p.[DateCreated],p.[Image],p.[RoleWeight]
		,p.[RoleOperation],p.[Description],p.[LookingFor],p.[MembersCache],p.[UserCache]
		,p.[LocationDetailCache],p.[RelationshipStatus],p.[NSFW],p.[ShowPublic],p.[Latitude],p.[Longitude]
		,p.[VerificationsCache]*/
	    -- ,(SELECT * FROM [dbo].[ProfileMembers] pm WHERE pm.ProfileUUID = p.UUID   ) AS Members --ORDER By pm.SortOrder 
FROM [dbo].[Profiles] p
WHERE 
		p.UUID = @UUID AND 
	    (p.Private = 0 ) AND
	    (p.Deleted = 0 );

SELECT * FROM [dbo].[ProfileMembers] pm WHERE pm.ProfileUUID = @UUID;

SELECT * FROM [dbo].Locations l 
LEFT JOIN [dbo].[Profiles] p on p.LocationUUID = l.UUID
WHERE p.UUID = @UUID;

SELECT * FROM [dbo].[Attributes] a WHERE a.ReferenceUUID = @UUID AND a.ReferenceType = 'Profile';

SELECT [UUID]      ,[UUIDType]      ,[VerificationDate]      ,[RecipientUUID]      ,[RecipientProfileUUID]
      ,[RecipientAccountUUID]           ,[RecipientLocationUUID]      ,[VerifierUUID]
      ,[Points]      ,[Deleted]     ,[VerificationType]   
	  -- ,[RecipientIP] ,[VerifierIP]      ,[VerifierProfileUUID]      ,[VerifierAccountUUID]      ,[VerifierRoleUUID]      ,[VerifierLocationUUID]       
       --,[VerifierLatitude]      ,[VerifierLongitude]      ,[RecipientLatitude]      ,[RecipientLongitude]
	   -- ,[Weight]      ,[Multiplier]   ,[VerificationTypeMultiplier]      ,[DateDeleted]
  FROM [Prod_platoscom].[dbo].[UserVerificationLog]
  WHERE RecipientProfileUUID = @UUID;

-- --LEFT JOIN [dbo].[Locations] l on l.UUID = p.LocationUUID -- AS LocationDetail

/* p.UUID IN (SELECT pm.Name FROM [dbo].[ProfileMembers] pm WHERE pm.ProfileUUID = p.UUID   ) 

    profile.Members = string.IsNullOrWhiteSpace(profile.MembersCache) ? context.GetAll<ProfileMember>().Where(w => w.ProfileUUID == profile.UUID).OrderBy(o => o.SortOrder).ToList() :
                                                                                    JsonConvert.DeserializeObject<List<ProfileMember>>(profile.MembersCache);
                profile.User = string.IsNullOrWhiteSpace(profile.UserCache) ? context.GetAll<User>().FirstOrDefault(w => w.UUID == profile.UserUUID) :
                                                                                    JsonConvert.DeserializeObject<User>(profile.UserCache);
                profile.LocationDetail = string.IsNullOrWhiteSpace(profile.LocationDetailCache) ? context.GetAll<Location>().FirstOrDefault(w => w.UUID == profile.LocationUUID) :
                                                                                                    JsonConvert.DeserializeObject<Location>(profile.LocationDetailCache);

                profile.Attributes = context.GetAll<TMG.Attribute>()?.Where(w => w.AccountUUID == profile.AccountUUID
                          && w.ReferenceUUID == profile.UUID && w.ReferenceType.EqualsIgnoreCase(profile.UUIDType)).ToList();


						  --LEFT JOIN [dbo].[ProfileMembers] pm ON pm.ProfileUUID = p.UUID  -- ORDER By pm.SortOrder
	
*/

-- select * from profiles where uuid = '71eb539c184149f39e5d69bd77825a3b'