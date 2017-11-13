using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml;
using System.Xml.Xsl;

using Comun;
using EjecutableEncriptador;
using Encriptador;
using MaquinaDeEstados;
using QRCodeLib;

namespace cfd.FacturaElectronica
{
    class cfdFacturaXmlWorker : BackgroundWorker
    {
        private Parametros _Param;
        private ConexionAFuenteDatos _Conex;
        public string ultimoMensaje = "";

        public cfdFacturaXmlWorker(ConexionAFuenteDatos Conex, Parametros Param)
        {
            WorkerReportsProgress = true;
            WorkerSupportsCancellation = true;
            _Param = Param;
            _Conex = Conex;
        }

        /// <summary>
        /// Ejecuta la generaci�n de archivos xml y pdf en un thread independiente
        /// </summary>
        /// <param name="e">trxVentas</param>
        protected override void OnDoWork(DoWorkEventArgs e)
        {
            try
            {
                String msj;
                ReportProgress(0, "Iniciando proceso...\r\n");
                object[] args = e.Argument as object[];
                vwCfdTransaccionesDeVenta trxVenta = (vwCfdTransaccionesDeVenta)args[0];
                trxVenta.Rewind();                                                          //move to first record

                int errores = 0; int i = 1;
                string antiguoIdCertificado = "";
                XmlDocument sello = new XmlDocument();
                TecnicaDeEncriptacion criptografo = null;
                XmlDocument comprobante = new XmlDocument();
                XmlDocumentFragment addenda;
                cfdReglasFacturaXml DocVenta = new cfdReglasFacturaXml(_Conex, _Param);     //log de facturas xml emitidas y anuladas
                ReglasME maquina = new ReglasME(_Param);
                ValidadorXML validadorxml = new ValidadorXML(_Param);
                TransformerXML loader = new TransformerXML();
                XslCompiledTransform xslCompilado = loader.Load(_Param.URLArchivoXSLT);
                PAC representanteSat = new PAC(trxVenta.Ruta_clavePac, trxVenta.Contrasenia_clavePac, _Param);
                String Sello = string.Empty;

                if (DocVenta.numMensajeError != 0 )
                {
                    e.Result = DocVenta.ultimoMensaje + "\r\n";
                    ReportProgress(100, DocVenta.ultimoMensaje + "\r\n");
                    return;
                }
                do
                {
                    msj = String.Empty;
                    try
                    {
                        if (CancellationPending) { e.Cancel = true; return; }

                        if (trxVenta.Estado.Equals("no emitido") &&
                            maquina.ValidaTransicion(_Param.tipoDoc, "EMITE XML Y PDF", trxVenta.EstadoActual, "emitido/impreso"))
                            if (trxVenta.Voidstts == 0)  //documento no anulado
                            {
                                //Cargar los datos del certificado por cada nuevo Id de certificado asociado al documento de venta
                                if (!trxVenta.ID_Certificado.Equals(antiguoIdCertificado))
                                {
                                    criptografo = new TecnicaDeEncriptacion(trxVenta.Ruta_clave, trxVenta.Contrasenia_clave, trxVenta.Ruta_certificado.Trim(), trxVenta.Ruta_certificado.Replace(".cer", ".pem").Trim());
                                    antiguoIdCertificado = trxVenta.ID_Certificado;
                                }

                                comprobante.LoadXml(trxVenta.ComprobanteXml);
                                comprobante.DocumentElement.SetAttribute("NoCertificado", criptografo.noCertificado);

                                if (criptografo.numErrores == 0)
                                {
                                    loader.getCadenaOriginal(comprobante, xslCompilado);    //Obtener cadena original del CFD
                                    Sello = criptografo.obtieneSello(loader.cadenaOriginal);//Crear el archivo xml y sellarlo
                                    comprobante.DocumentElement.SetAttribute("Sello", Sello);
                                    comprobante.DocumentElement.SetAttribute("Certificado", criptografo.certificadoFormatoPem);

                                    if (!_Conex.IntegratedSecurity)                         //para testeo:
                                        comprobante.Save(new XmlTextWriter(trxVenta.Sopnumbe + "tst.xml", Encoding.UTF8));
                                }

                                if (criptografo.numErrores == 0)
                                {
                                    validadorxml.ValidarXSD(comprobante);                   //Validar el esquema del archivo xml
                                    representanteSat.comprobanteFiscal = comprobante;
                                    representanteSat.timbraCFD();                           //agregar sello al comprobante
                                }
                                else
                                    errores++;

                                if (criptografo.numErrores == 0)
                                {
                                    //Agregar el nodo addenda si existe
                                    //if (trxVenta.Addenda != null && trxVenta.Addenda.Length > 0)
                                    //{
                                    //    addenda = comprobante.CreateDocumentFragment();
                                    //    addenda.InnerXml = trxVenta.Addenda;
                                    //    comprobante.DocumentElement.AppendChild(addenda);
                                    //}

                                    //Guarda el archivo xml, genera el cbb y el pdf. 
                                    //Luego anota en la bit�cora la factura emitida o el error al generar cbb o pdf.
                                    if (!DocVenta.AlmacenaEnRepositorio(trxVenta, comprobante, maquina, representanteSat.Uuid, Sello))
                                        errores++;
                                }
                                else
                                    errores++;
                                msj = criptografo.ultimoMensaje + " " + DocVenta.ultimoMensaje;
                            }
                            else //si el documento est� anulado en gp, agregar al log como emitido
                            {
                                maquina.ValidaTransicion("FACTURA", "ANULA VENTA", trxVenta.EstadoActual, "emitido");
                                msj = "Anulado en GP y marcado como emitido.";
                                DocVenta.RegistraLogDeArchivoXML(trxVenta.Soptype, trxVenta.Sopnumbe, "Anulado en GP", "0", _Conex.Usuario, "",
                                                                         "emitido", maquina.eBinarioNuevo, msj.Trim());
                            }
                    }
                    catch (Exception lo)
                    {
                        string imsj = lo.InnerException == null ? "" : lo.InnerException.ToString();
                        msj = lo.Message + " " + imsj + "\r\n" + lo.StackTrace;
                    }
                    finally
                    {
                        ReportProgress(i * 100 / trxVenta.RowCount, "Doc:" + trxVenta.Sopnumbe + " " + msj.Trim() + "\r\n");
                        i++;
                    }
                } while (trxVenta.MoveNext() && errores < 10);
            }
            catch (Exception xw)
            {
                string imsj = xw.InnerException == null ? "" : xw.InnerException.ToString();
                this.ultimoMensaje = xw.Message + " " + imsj + "\r\n" + xw.StackTrace;
            }
            finally
            {
                ReportProgress(100, ultimoMensaje);
            }
            e.Result = "Generaci�n de archivos finalizado! \r\n ";
            ReportProgress(100, "");
        }

    }
}
