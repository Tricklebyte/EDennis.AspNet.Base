﻿create view AspNetRolesView as
	select r.Id, r.Name, r.NormalizedName, r.ApplicationId, a.Name ApplicationName,
	r.SysUser, r.SysStatus, r.SysStart, r.SysEnd, r.Properties
		from dbo.AspNetRoles r
        inner join dbo.AspNetApplications a
            on r.ApplicationId = a.Id
go