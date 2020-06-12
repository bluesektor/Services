 -- Find duplicates in location
  SELECT up.UUID, up.Name , count(*) as NumDuplicates FROM
	 locations up
	   Group by up.UUID, up.Name
	   having count(*) > 1
	   order by Name