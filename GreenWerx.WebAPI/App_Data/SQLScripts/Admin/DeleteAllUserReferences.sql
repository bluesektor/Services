
DELETE FROM [dbo].[Users] WHERE	UUID					= '9bf2b8b061db411e88eaf9bcf155da14';
DELETE FROM [dbo].[Users]			WHERE UUID			= '9bf2b8b061db411e88eaf9bcf155da14';
DELETE FROM [dbo].[UserSessions]	WHERE UserUUID		= '9bf2b8b061db411e88eaf9bcf155da14';
DELETE FROM [dbo].[UsersInRoles]	WHERE ReferenceUUID	= '9bf2b8b061db411e88eaf9bcf155da14';
DELETE FROM [dbo].[EmailLog]		WHERE UserUUID		= '9bf2b8b061db411e88eaf9bcf155da14';
DELETE FROM [dbo].[Favorites]		WHERE UserUUID		= '9bf2b8b061db411e88eaf9bcf155da14';
DELETE FROM [dbo].[ProfileMembers]	WHERE UserUUID		= '9bf2b8b061db411e88eaf9bcf155da14';
DELETE FROM [dbo].[Profiles]		WHERE UserUUID		= '9bf2b8b061db411e88eaf9bcf155da14';
DELETE FROM [dbo].[RolesBlocked]	WHERE ReferenceUUID	= '9bf2b8b061db411e88eaf9bcf155da14';
DELETE FROM [dbo].[ShoppingCarts]	WHERE UserUUID		= '9bf2b8b061db411e88eaf9bcf155da14';



DELETE FROM [dbo].[Accounts]	WHERE UUID		= '873d255f7b434fee8c6a51ec782e2bed';