
 /* UserManager um = new UserManager(Globals.DBConnectionKey, Request.Headers?.Authorization?.Parameter);
            string toName = um.GetUserByEmail(EmailMessage.EmailTo)?.Name; 
            string fromName = um.GetUserByEmail(form.SentFrom)?.Name; 
            EmailMessage.Name = EmailMessageManager.GetNameField(toName, fromName);
			*/

		-- select * from Users  where email ='JQ8E9yA7T5JRkv4I16zlGyAJryKjU9cNXRcCq/qqjeYuKJ99iSou3mC1X9T9k2e6'
	
		--GatosLocos email ='zyVf6KzvlSoZ/eCfzQVUV94gQ3QCrqw0N+p+MuR6tgtrLWYOKFrvRXJtX7SNucRwTogjObLFoeZK7pizLNfaGA=='
		 
		 -- sand  email = 'JQ8E9yA7T5JRkv4I16zlGyAJryKjU9cNXRcCq/qqjeYuKJ99iSou3mC1X9T9k2e6'  sand uuid 0c9d962ce1b64b22a11ea3a815ce1185
		 -- blockedSM email ='2mxeHVd9xfqM6PJ3cKJq+gDcnnh+bfe3uaYMsii4fwtVcmeWh8Uar/s0pJFfybtx0uBvfNi9KV/byolfLlCGog==' blockedSM 8e7e2fb093a245278f97dd1cb4d6789e


	/*
update [Prod_platoscom].[dbo].[users] set email = 'zyVf6KzvlSoZ/eCfzQVUV94gQ3QCrqw0N+p+MuR6tgtrLWYOKFrvRXJtX7SNucRwTogjObLFoeZK7pizLNfaGA==' where uuid =  '0c9d962ce1b64b22a11ea3a815ce1185'	
update [Prod_platoscom].[dbo].[EmailLog] set NameFrom = 'sand'	where EmailFrom = 'JQ8E9yA7T5JRkv4I16zlGyAJryKjU9cNXRcCq/qqjeYuKJ99iSou3mC1X9T9k2e6' 

update [Prod_platoscom].[dbo].[users] set email = 'zyVf6KzvlSoZ/eCfzQVUV94gQ3QCrqw0N+p+MuR6tgtrLWYOKFrvRXJtX7SNucRwTogjObLFoeZK7pizLNfaGA==' where uuid =  '8e7e2fb093a245278f97dd1cb4d6789e'	
update [Prod_platoscom].[dbo].[EmailLog] set NameFrom = 'blockedSM'	where EmailFrom = 'zyVf6KzvlSoZ/eCfzQVUV94gQ3QCrqw0N+p+MuR6tgtrLWYOKFrvRXJtX7SNucRwTogjObLFoeZK7pizLNfaGA==' 


update [Prod_platoscom].[dbo].[EmailLog] set NameTo = 'gatoslocos'	where EmailTo = 'zyVf6KzvlSoZ/eCfzQVUV94gQ3QCrqw0N+p+MuR6tgtrLWYOKFrvRXJtX7SNucRwTogjObLFoeZK7pizLNfaGA==' 
update [Prod_platoscom].[dbo].[EmailLog] set NameFrom = 'gatoslocos'	where emailfrom = 'zyVf6KzvlSoZ/eCfzQVUV94gQ3QCrqw0N+p+MuR6tgtrLWYOKFrvRXJtX7SNucRwTogjObLFoeZK7pizLNfaGA=='

update [Prod_platoscom].[dbo].[users] set email = 'JQ8E9yA7T5JRkv4I16zlGyAJryKjU9cNXRcCq/qqjeYuKJ99iSou3mC1X9T9k2e6' where uuid =  'a254990594214f6aba2250875d947ab4'	
update [Prod_platoscom].[dbo].[EmailLog] set NameTo = 'ant'	where EmailTo = 'JQ8E9yA7T5JRkv4I16zlGyAJryKjU9cNXRcCq/qqjeYuKJ99iSou3mC1X9T9k2e6' 
*/

-- update [Prod_platoscom].[dbo].[EmailLog] set Deleted = 0
-- update [Prod_platoscom].[dbo].[EmailLog] set DateRead = null

-- GatosLocos email ='zyVf6KzvlSoZ/eCfzQVUV94gQ3QCrqw0N+p+MuR6tgtrLWYOKFrvRXJtX7SNucRwTogjObLFoeZK7pizLNfaGA=='
SELECT UUID, nameFrom, nameto, EmailFrom, EmailTo, DateSent, DateCreated, Deleted
  FROM [Prod_platoscom].[dbo].[EmailLog] order by DateSent desc

  select * from  EmailLog where emailfrom = 'zyVf6KzvlSoZ/eCfzQVUV94gQ3QCrqw0N+p+MuR6tgtrLWYOKFrvRXJtX7SNucRwTogjObLFoeZK7pizLNfaGA=='