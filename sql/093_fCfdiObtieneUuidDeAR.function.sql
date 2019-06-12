IF OBJECT_ID ('dbo.fCfdiObtieneUUIDDeAR') IS NOT NULL
   DROP FUNCTION dbo.fCfdiObtieneUUIDDeAR
GO

create function dbo.fCfdiObtieneUUIDDeAR(@RMDTYPAL smallint, @DOCNUMBR varchar(21), @CUSTNMBR varchar(15))
returns table
as
--Prop�sito. Devuelve el UUID de una factura AR. Este dato debe estar en el campo nota.
--Requisitos. 
--12/06/19 jcf Creaci�n 
--
return
(
	select substring(ni.txtfield, 1, 40) uuid, rmx.voidstts
		from dbo.vwRmTransaccionesTodas rmx
		inner join sy03900 ni
			on ni.noteindx = rmx.noteindx
	where rmx.RMDTYPAL = 1			-- 1 invoice
		and rmx.rmdTypAl = @RMDTYPAL
       and rmx.DOCNUMBR = @DOCNUMBR
	   and rmx.custnmbr = @CUSTNMBR
)
go


IF (@@Error = 0) PRINT 'Creaci�n exitosa de la funci�n: fCfdiObtieneUUIDDeAR()'
ELSE PRINT 'Error en la creaci�n de la funci�n: fCfdiObtieneUUIDDeAR()'
GO

-------------------------------------------------------------------------------------------------------------
--select *
--from dbo.fCfdiObtieneUUIDDeAR(3, '00000002')

