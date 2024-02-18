using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRToolKit.DTO
{
    internal class CandidateDTO
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public List<QualificationDTO> Qualification { get; set; }
        public List<WorkHistoryDTO> WorkHistory { get; set; }
        public string Summary { get; set; }
    }

    public class WorkHistoryDTO
    {
        public string Company { get; set; }
        public string Position { get; set; }
        public string Duration { get; set; }
        public string Location { get; set; }
        public string Dates { get; set; }
        public string Summary { get; set; }
    }

    public class QualificationDTO
    {
        public string Degree { get; set; }
        public string Field { get; set; }
        public string University { get; set; }
        public int Year { get; set; }
        public string Certification { get; set; }
    }
}
