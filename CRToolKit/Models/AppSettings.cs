using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourcingToolKit.Models
{
    internal class AppSettings
    {
        [SQLite.PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string TemplateFile { get; set; }
        public string ProcessingFolder { get; set; }
        
    }
}
