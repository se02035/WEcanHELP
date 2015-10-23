using System.Collections.Generic;

namespace DataLayer
{
    public class AddAssetViewModel
    {
        public virtual ICollection<Application> Applications { get; set; }
        public virtual Asset Asset { get; set; }
    }
}
