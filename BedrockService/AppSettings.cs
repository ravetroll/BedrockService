using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace BedrockService
{
    public class XmlConfigurationSection : ConfigurationSection
    {
        // This may be fetched multiple times: XmlReaders can't be reused, so load it into an XDocument instead
        private XDocument document;

        protected override void DeserializeSection(XmlReader reader)
        {
            this.document = XDocument.Load(reader);
        }

        protected override object GetRuntimeObject()
        {
            // This is cached by ConfigurationManager, so no point in duplicating it to stop other people from modifying it
            return this.document;
        }
    }

    public class AppSettings
    {
        private const string sectionName = "settings";
        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(AppSettings), new XmlRootAttribute(sectionName));

        public static readonly AppSettings Instance;

        static AppSettings()
        {
            var document = (XDocument)ConfigurationManager.GetSection(sectionName);
            Instance = (AppSettings)serializer.Deserialize(document.CreateReader());
        }

        // Add your custom fields in here....

        

        [XmlElement("ServerConfig")]
        public List<ServerConfig> ServerConfig { get; set; }

        [XmlElement("BackupConfig")]
        public BackupConfig BackupConfig { get; set; }
       
    }

    public class ServerConfig
    {
        public string ServerName { get; set; }

        public string ServerPort4 {  get; set; }

        public string ServerPort6 { get; set; }
        public string BedrockServerExeLocation { get; set; }
        public string BackupFolderName { get; set; }
        public bool Primary { get; set; }
    }

    public class BackupConfig
    {
        public bool BackupOn { get; set; }
        
        public long BackupIntervalMinutes { get; set; }
    }


   
}
