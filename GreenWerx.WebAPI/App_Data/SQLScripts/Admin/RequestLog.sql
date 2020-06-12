


SELECT top 500 rl.IPAddress, rl.Referrer,  count(*) as NumDuplicates FROM
		RequestLogs rl
	  -- JOIN dbo.AspNetUsers u ON u.ID = up.UserID
	   Group by rl.IPAddress, rl.Referrer 
	   having count(*) > 1
	   order by
	  -- rl.Referrer
	    rl.IPAddress
	   --order by LastName, FirstName


	  select * from REquestlogs where IPAddress = '172.56.17.255' -- 2019-07-25 01:14:00 genie