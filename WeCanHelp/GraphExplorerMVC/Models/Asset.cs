using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphExplorerMVC.Models
{
    public class Asset
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string RawUrl { get; set; }
        public string Published { get; set; }
        public virtual Application Application { get; set; }

    }
}
