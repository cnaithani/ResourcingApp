using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRToolKit.Models
{
    internal class Candidate
    {
        [SQLite.PrimaryKey]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Qualification { get; set; }
        public List<WorkHistory> Work_History { get; set; }
        public int Rating { get; set; } 
        public string FilePath { get; set; }
        public DateTime Modified { get; set; }
    }
}
