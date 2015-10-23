using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphExplorerMVC.Models
{
    public class OneDriveFile
    {
        public string Name { get; set; }
        public string SourceId { get; set; }
        public string WebUrl { get; set; }
        public bool IsSelected { get; set; }
    }
}
