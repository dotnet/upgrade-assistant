using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.UpgradeAssistant.Steps.Configuration
{
    public class ConfigFile
    {
        public ConfigFile(string path)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Contents = XDocument.Load(path, LoadOptions.SetLineInfo);
        }

        public string Path { get; set; }

        public XDocument Contents { get; set; }
    }
}
