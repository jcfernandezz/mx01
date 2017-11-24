IF OBJECT_ID ('dbo.fCfdiPagoSimultaneoMayor') IS NOT NULL
   DROP FUNCTION dbo.fCfdiPagoSimultaneoMayor
GO

create function dbo.fCfdiPagoSimultaneoMayor(@soptype smallint, @sopnumbe varchar(21))
returns table
--Prop�sito. Obtiene la forma de pago del pago m�s grande. Este pago ha sido ingresado simult�neamente con la factura.
--24/10/17 jcf Creaci�n
--
as
return(
	select top (1) 
		case when left(UPPER(cm.CMUSRDF1), 2) = 'CB' then	--ch representa una cuenta bancaria
 			case py.pymttype 
 				when 4 then '03'					--transf. electr�nica
 				when 5 then '02'					--cheque
 				when 6 then left(py.cardname,2)	--tarjeta
				else null 
			end
			else									--representa un medio de pago
 				left(Rtrim(cm.CMUSRDF1), 2)
		end	FormaPago
	from sop10103 py
		left join CM00100 cm
			on cm.chekbkid = py.chekbkid
	where py.soptype = @soptype
	and py.sopnumbe = @sopnumbe
	order by py.oamtpaid desc
)

go
IF (@@Error = 0) PRINT 'Creaci�n exitosa de: fCfdiPagoSimultaneoMayor()'
ELSE PRINT 'Error en la creaci�n de: fCfdiPagoSimultaneoMayor()'
GO
--------------------------------------------------------------------------------------------------------

