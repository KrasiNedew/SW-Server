use SimpleWarsContext
go

create procedure LogoutUser @id int
as
	set nocount on
	update Players
	set LoggedIn = 0
	from Players
	where @id = Id
go