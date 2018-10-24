/*
SELECT TOP 100
	u.RequestId, 
	u.Upvotes,
	d.Downvotes
FROM 
	(
		SELECT 
			SongRequest.Id AS RequestId,
			Count(u.Vote) as Upvotes 
		FROM 
			SongRequest
		INNER JOIN 
			SongRequestUserVotes 
			AS u
			ON SongRequest.Id = u.RequestId
			GROUP BY 
				SongRequest.Id, 
				Vote
			HAVING Vote > 0
		) 
	AS u
	INNER JOIN 
		(
		*/
		SELECT 
			d.RequestId,
			sr.Title,
			sr.Artist,
			Count(d.Vote) 
				AS Downvotes 
		FROM 
			SongRequest 
				AS sr
		JOIN 
			SongRequestUserVotes 
				AS d
			ON sr.Id = d.RequestId
			GROUP BY 
				d.RequestId, 
				sr.Title,
				sr.Artist,
				d.Vote
			HAVING d.Vote < 0
		/*
		) 
		AS d 
		ON d.RequestId = u.RequestId

			*/