
-- Requestor: blockedSM
-- Target: Gatos
DECLARE @requestingUserUUID			varchar(32)	= '8e7e2fb093a245278f97dd1cb4d6789e';
DECLARE @requestingAccountUUID		varchar(32)	= 'baf08adcfacf4cdeab6f88d5293b6c53';
DECLARE @targetProfileUserUUID		varchar(32) = '8ac0adc1e7154afda15069c822d68d6d';
DECLARE @targetProfileAccountUUID	varchar(32) = 'c66185a39e1c475a93ba5b053ae31d23';


-- DidRequestorBlockTarget(...)
-- Requestor => blocked => Target
-- this user (blockedSM - sm) has block the cpl role below (gatos). this should come back as blocked.
-- this query is for the blockedSM accessing the gatos profile (should be blocked).

--var targetRoles = _roleManager.GetAssignedRoles(targetProfile.UserUUID, targetProfile.AccountUUID);// returns UserRoles
SELECT *
FROM [Prod_platoscom].[dbo].[UsersInRoles] tUIR
WHERE tUIR.ReferenceUUID =  @targetProfileUserUUID AND tUIR.AccountUUID = @targetProfileAccountUUID  AND tUIR.Deleted = 0 
	    --                                  AND Name = RoleFlags.MemberRoleNames.SingleMale

-- var requestorsBlockedRoles = _roleManager.GetBlockedRoles(_requestingUser.UUID, _requestingUser.AccountUUID);
SELECT *  -- should return 1: "Block Couples"
FROM  [Prod_platoscom].[dbo].[RolesBlocked] rb
WHERE AccountUUID = @requestingAccountUUID AND 
	  ReferenceUUID  = @requestingUserUUID;



-- DidTargetBlockRequestor(...)
-- Target => blocked => Requestor
-- inverse from above. gatos accessing blockedsm should be blocked
--  var requestorRoles = _roleManager.GetAssignedRoles(_requestingUser.UUID, _requestingUser.AccountUUID);// returns UserRoles
SELECT *
FROM [Prod_platoscom].[dbo].[UsersInRoles] tUIR
WHERE tUIR.ReferenceUUID =  @requestingUserUUID AND tUIR.AccountUUID = @requestingAccountUUID  AND tUIR.Deleted = 0 


--  var targetsBlockedRoles = _roleManager.GetBlockedRoles(targetProfile.UserUUID, targetProfile.AccountUUID);
SELECT *  -- should return 1: "Block Couples"
FROM  [Prod_platoscom].[dbo].[RolesBlocked] rb
WHERE AccountUUID = @targetProfileAccountUUID AND 
	  ReferenceUUID  = @targetProfileUserUUID;