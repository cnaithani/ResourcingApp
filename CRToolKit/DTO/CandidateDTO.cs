using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CRToolKit.DTO
{
    public class CandidateDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public int Rating { get; set; }
        public string Address { get; set; }
        public string Linkedin { get; set; }
        public List<QualificationDTO> Qualification { get; set; }
        public List<WorkHistoryDTO> WorkHistory { get; set; }
        public string Summary { get; set; }
        public string KeySkills { get; set; }
        public string FilePath { get; set; }
    }

    public class WorkHistoryDTO
    {
        public string Company { get; set; }
        public string Employer {  get; set; }   
        public string Position { get; set; }
        public string Duration { get; set; }
        public string Location { get; set; }
        public string Dates { get; set; }
        public string Summary { get; set; }
    }

    public class QualificationDTO
    {
        public string Degree { get; set; }
        public string Certification { get; set; }
        public string Field { get; set; }
        public string University { get; set; }
        public string Month { get; set; }
        public string Year { get; set; }
        public string Location { get; set; }
        public string DisplayDate { get; set; }
        public string Summary { get; set; }
    }

    public static class JSONTransformer
    {
        public static void Transform(CandidateDTO candidate)
        {
            if (candidate.WorkHistory.Count > 0)
            {
                foreach (var item in candidate.WorkHistory)
                {
                    if (string.IsNullOrEmpty(item.Company) && !string.IsNullOrEmpty(item.Employer)){
                        item.Company = item.Employer;
                    }
                }
            }

            if (candidate.Qualification.Count > 0)
            {
                foreach (var item in candidate.Qualification)
                {
                    GetDates(item);
                    GetSummary(item);
                }
            }

            static void GetDates(QualificationDTO item)
            {
                string monthYearPattern = @"([A-Za-z]+)\s*(\d{4})";
                string yearPattern = @"(\d{4})";
                var month = string.Empty;
                var year = string.Empty;
                var fullDate = string.Empty;
                // Try to match month and year
                Match monthYearMatch = Regex.Match(item.Year, monthYearPattern);
                Match yearMatch = Regex.Match(item.Year, yearPattern);

                if (monthYearMatch.Success)
                {
                    month = monthYearMatch.Groups[1].Value;
                    year = monthYearMatch.Groups[2].Value;
                    fullDate = string.Concat(month, ", ", year);
                }
                else if (yearMatch.Success)
                {
                    year = yearMatch.Groups[1].Value;
                    fullDate = year;
                    //TODO: Log issue - Low
                }
                item.Month = month;
                item.Year = year;
                item.DisplayDate = fullDate;
            }

            static void GetSummary(QualificationDTO qual)
            {
                string summary = "";

                if (!string.IsNullOrEmpty(qual.Degree))
                    summary += $"{qual.Degree} ";

                if (!string.IsNullOrEmpty(qual.Certification))
                    summary += $"({qual.Certification}) ";

                if (!string.IsNullOrEmpty(qual.Field))
                    summary += $"in {qual.Field} ";

                if (!string.IsNullOrEmpty(qual.University))
                    summary += $"from {qual.University}, ";

                if (!string.IsNullOrEmpty(qual.Location))
                    summary += $"{qual.Location} ";

                if (!string.IsNullOrEmpty(qual.DisplayDate))
                    summary += $"in {qual.DisplayDate}";

                qual.Summary =  summary.Trim();
            }

        }
    }
}
