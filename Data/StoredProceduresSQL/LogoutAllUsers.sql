use SimpleWarsContext
go

create procedure LogoutAllUsers
as
	set nocount on;
	update Players
	set LoggedIn = 0, UnderAttack = 0;
go



