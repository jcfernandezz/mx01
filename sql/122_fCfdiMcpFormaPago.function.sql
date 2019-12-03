-----------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID ('dbo.fCfdiMcpFormaPago') IS NOT NULL
   DROP FUNCTION dbo.fCfdiMcpFormaPago
GO

create function dbo.fCfdiMcpFormaPago(@DOCNUMBR varchar(21))
returns table
--Prop�sito. Obtiene la forma de pago de MCP
--10/11/17 jcf Creaci�n
--14/02/18 jcf Agrega nfmcp30100
--
as
return(
	select top (1) mcpd.grupid, mcpfp.tii_chekbkid
	from
		( select tii_chekbkid, medioid, numberie, lineamnt  
		from nfmcp20100  
		union all
		select tii_chekbkid, medioid, numberie, lineamnt  
		from nfmcp30100  
		) mcpfp
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

