using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphExplorerMVC.Models
{
    public class AddAssetViewModel
    {
        public virtual ICollection<Application> Applications { get; set; }
        public virtual Asset Asset { get; set; }
    }
}
