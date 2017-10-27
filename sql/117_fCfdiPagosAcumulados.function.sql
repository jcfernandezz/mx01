IF OBJECT_ID ('dbo.fCfdiPagosAcumulados') IS NOT NULL
   DROP FUNCTION dbo.fCfdiPagosAcumulados
GO

create function dbo.fCfdiPagosAcumulados(@PAGOTIPO smallint, @PAGONUM varchar(21), @PAGOFECHA DATETIME, @FACTURATIPO smallint, @FACTURANUM varchar(21), @TRXDSCRN char(31))
returns table
--Prop�sito. Obtiene la suma de pagos aplicados a una factura y el n�mero de cuota
--27/10/17 jcf Creaci�n
--
as
return(
		SELECT COUNT(*) +1 numCuota, SUM(X.ORAPTOAM) AS sumaDePagosAplicados
		FROM dbo.vwCfdiRmTrxAplicadas X
			inner join vwRmTransaccionesTodas rm
			on X.APFRDCNM = rm.docnumbr
			and X.APFRDCTY = rm.RMDTYPAL
			AND rm.VOIDSTTS = 0 
		WHERE X.APTODCNM = @FACTURANUM
		and X.APTODCTY = @FACTURATIPO
		AND X.idPago <= convert(varchar(12), @PAGOFECHA, 112) + rtrim(@PAGONUM)
		AND X.APFRDCTY = @PAGOTIPO
)
go

IF (@@Error = 0) PRINT 'Creaci�n exitosa de: fCfdiPagosAcumulados'
ELSE PRINT 'Error en la creaci�n de: fCfdiPagosAcumulados'
GO

--------------------------------------------------------------------------------------------------------------------------------------------------------

