using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MultirIntegraModulab
{
    /// <summary>
    /// Servei per consultar dades de pacients a través del web service Flamma
    /// La URL i el timeout es configuren segons l'entorn (producció o preproducció)
    /// </summary>
    public class PacientWebService : IDisposable
    {
        private readonly string _webServiceUrl;
        private readonly HttpClient _httpClient;

        public PacientWebService(string webServiceUrl, int timeoutSeconds = 30)
        {
            _webServiceUrl = webServiceUrl ?? throw new ArgumentNullException(nameof(webServiceUrl));
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        }

        /// <summary>
        /// Consulta les dades d'un pacient al web service Flamma
        /// </summary>
        /// <param name="pacientSap">Identificador SAP del pacient</param>
        /// <returns>Dades del pacient o null si no es troba</returns>
        public async Task<DadesPacientWebService> ConsultarPacientAsync(string pacientSap)
        {
            try
            {
                //Logger.Info($"         Consultant pacient {pacientSap} al web service de Pacients de SAP ...");

                string soapRequest = CrearSoapRequest(pacientSap);
                var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");

                // Afegir headers necessaris per SOAP
                content.Headers.Add("SOAPAction", "urn:xmethods-delayed-quotes#consultaPacient");

                var response = await _httpClient.PostAsync(_webServiceUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    return ProcessarResposta(responseContent, pacientSap);
                }
                else
                {
                    Logger.Error($"Error en la resposta del web service: {response.StatusCode} - {response.ReasonPhrase}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error consultant pacient {pacientSap}: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Crea la petició SOAP per consultar un pacient
        /// </summary>
        private string CrearSoapRequest(string pacientSap)
        {
            return $@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:urn=""urn:xmethods-delayed-quotes"">
   <soapenv:Header/>
   <soapenv:Body>
      <urn:consultaPacient>
         <centre>DT</centre>
         <pacient>{pacientSap}</pacient>
         <user>webservice</user>
         <pwd>wsicsgir</pwd>
         <sistema>sarch</sistema>
         <cognom1></cognom1>
         <cognom2></cognom2>
         <nom></nom>
      </urn:consultaPacient>
   </soapenv:Body>
</soapenv:Envelope>";
        }

        /// <summary>
        /// Processa la resposta SOAP del web service
        /// </summary>
        private DadesPacientWebService ProcessarResposta(string responseXml, string pacientSap)
        {
            try
            {
                Logger.Info($"  Processant resposta XML per pacient {pacientSap}...");
                
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(responseXml);

                // Buscar l'element de resposta SOAP
                var responseNode = xmlDoc.SelectSingleNode("//ns1:consultaPacientResponse", CrearNamespaceManager(xmlDoc));
                if (responseNode == null)
                {
                    // Intentar sense namespace
                    responseNode = xmlDoc.SelectSingleNode("//consultaPacientResponse");
                }
                
                if (responseNode == null)
                {
                    Logger.Warning($" ❌ No s'ha trobat element consultaPacientResponse per pacient {pacientSap}");
                    return null;
                }

                // Extreure el contingut del node Result
                var resultNode = responseNode.SelectSingleNode("Result");
                if (resultNode == null)
                {
                    Logger.Warning($" ❌ No s'ha trobat element Result per pacient {pacientSap}");
                    return null;
                }

                string xmlInteriorCodificat = resultNode.InnerText;
                if (string.IsNullOrWhiteSpace(xmlInteriorCodificat))
                {
                    Logger.Warning($" ❌ El node Result està buit per pacient {pacientSap}");
                    return null;
                }

                // Decodificar el XML interior (convertir entitats HTML a XML vàlid)
                string xmlInteriorDecodificat = System.Web.HttpUtility.HtmlDecode(xmlInteriorCodificat);
                
                Logger.Info($" ✔️ XML interior decodificat: {xmlInteriorDecodificat.Substring(0, Math.Min(200, xmlInteriorDecodificat.Length))} ...");

                // Processar el XML interior
                var xmlInterior = new XmlDocument();
                xmlInterior.LoadXml(xmlInteriorDecodificat);

                // Buscar l'element item dins de data
                var itemNode = xmlInterior.SelectSingleNode("//item");
                if (itemNode == null)
                {
                    Logger.Warning($"  ❌ No s'ha trobat element item dins del XML interior per pacient {pacientSap}");
                    return null;
                }

                // Comprovar si hi ha dades del pacient
                var nhcNode = itemNode.SelectSingleNode("NHC");
                if (nhcNode == null || string.IsNullOrWhiteSpace(nhcNode.InnerText))
                {
                    Logger.Info($" ❌ Pacient {pacientSap} no trobat al web service (NHC buit)");
                    return null;
                }

                // Extreure les dades del pacient del XML interior
                var pacient = new DadesPacientWebService
                {
                    PacientSap = pacientSap,
                    Nom = ObtenirValorNode(itemNode, "NOMBRE"),
                    Cognom1 = ObtenirValorNode(itemNode, "APELLIDO1"),
                    Cognom2 = ObtenirValorNode(itemNode, "APELLIDO2"),
                    Cip = ObtenirValorNode(itemNode, "CIP"),
                    Abs = ObtenirValorNode(itemNode, "ABS"),
                    Sexe = ObtenirValorNode(itemNode, "SEXE")
                };

                // Processar data de naixement
                string dataNaixStr = ObtenirValorNode(itemNode, "DNAIX");
                if (!string.IsNullOrWhiteSpace(dataNaixStr))
                {
                    if (DateTime.TryParse(dataNaixStr, out DateTime dataNaix))
                    {
                        pacient.DataNaixement = dataNaix;
                    }
                    else
                    {
                        Logger.Warning($" ❌ No s'ha pogut convertir la data de naixement '{dataNaixStr}' per pacient {pacientSap}");
                    }
                }

                Logger.Info($"  Pacient {pacientSap} trobat: {pacient.Nom} {pacient.Cognom1} {pacient.Cognom2} (CIP: {pacient.Cip})");
                return pacient;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error processant resposta XML per pacient {pacientSap}: {ex.Message}", ex);
                Logger.Error($"XML rebut: {responseXml.Substring(0, Math.Min(500, responseXml.Length))}...");
                return null;
            }
        }

        /// <summary>
        /// Crea un namespace manager per processar la resposta SOAP
        /// </summary>
        private XmlNamespaceManager CrearNamespaceManager(XmlDocument xmlDoc)
        {
            var namespaceManager = new XmlNamespaceManager(xmlDoc.NameTable);
            namespaceManager.AddNamespace("SOAP-ENV", "http://schemas.xmlsoap.org/soap/envelope/");
            namespaceManager.AddNamespace("ns1", "urn:xmethods-delayed-quotes");
            return namespaceManager;
        }

        /// <summary>
        /// Obté el valor d'un node XML de forma segura
        /// </summary>
        private string ObtenirValorNode(XmlNode parentNode, string nodeName)
        {
            var node = parentNode.SelectSingleNode(nodeName);
            return node?.InnerText?.Trim();
        }

        /// <summary>
        /// Alliberar recursos
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    /// <summary>
    /// Dades d'un pacient obtingudes del web service
    /// </summary>
    public class DadesPacientWebService
    {
        public string PacientSap { get; set; }
        public string Nom { get; set; }
        public string Cognom1 { get; set; }
        public string Cognom2 { get; set; }
        public DateTime? DataNaixement { get; set; }
        public string Sexe { get; set; }
        public string Cip { get; set; }
        public string Abs { get; set; }

        public override string ToString()
        {
            return $"Pacient {PacientSap}: {Nom} {Cognom1} {Cognom2} (CIP: {Cip})";
        }
    }
}