IF OBJECT_ID ('dbo.fCfdiMcpFormaPago') IS NOT NULL
   DROP FUNCTION dbo.fCfdiMcpFormaPago
GO

create function dbo.fCfdiMcpFormaPago(@DOCNUMBR varchar(21))
returns table
--Prop�sito. Obtiene la forma de pago de MCP
--10/11/17 jcf Creaci�n
--
as
return(
	select top (1) mcpd.grupid
	from  nfmcp20100 mcpfp 
 	left join nfmcp00700 mcpd 
		on mcpd.medioid=mcpfp.medioid
	where mcpfp.numberie = @DOCNUMBR
	order by mcpfp.lineamnt desc
)

go
IF (@@Error = 0) PRINT 'Creaci�n exitosa de: fCfdiMcpFormaPago()'
ELSE PRINT 'Error en la creaci�n de: fCfdiMcpFormaPago()'
GO
--------------------------------------------------------------------------------------------------------

