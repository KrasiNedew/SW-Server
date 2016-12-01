use SimpleWarsContext
go

create procedure LoginUser @id int
as 
	set nocount on
	update Players
	set LoggedIn = 1
	from Players
	where @id = Id
go