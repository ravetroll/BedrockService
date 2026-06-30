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

       
        public Command StartupCommands { get; set; }

        public int WCFPortNumber { get; set; }
    }

    public class BackupConfig
    {
        public bool BackupOn { get; set; }

        public string BackupIntervalCron { get; set; }

        // Tiered retention policy (restic/borg style). If omitted, every backup is kept.
        public RetentionConfig Retention { get; set; }
    }

    // Tiered retention, evaluated after every backup. A backup is kept if ANY rule
    // wants it; everything else is pruned. All counts default to 0 (rule inactive),
    // so an empty/omitted policy keeps everything. See BackupRetention for the rules.
    public class RetentionConfig
    {
        // Keep every backup newer than this age, e.g. "2h", "30m", "7d", "1w". Blank = off.
        public string KeepWithin { get; set; }

        // Keep the N most recent backups regardless of age.
        public int KeepLast { get; set; }

        // Keep the newest backup within each of the last N hours / days / weeks / months / years.
        public int KeepHourly { get; set; }
        public int KeepDaily { get; set; }
        public int KeepWeekly { get; set; }
        public int KeepMonthly { get; set; }
        public int KeepYearly { get; set; }
    }

    public class Command
    {
        [XmlElement("CommandText")]
        public List<string> CommandText { get; set; }
    }


   
}
