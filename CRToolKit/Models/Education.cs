using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRToolKit.Models
{
    internal class Education
    {
        [SQLite.PrimaryKey]
        public int Id { get; set; }
        public int CandidateId { get; set; }
        public string Name { get; set; }
        public string College { get; set; }
        public string Year { get; set; }
        public string Grade { get; set; }

    }
}
