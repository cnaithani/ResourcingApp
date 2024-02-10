using System;
namespace CRToolKit.Models
{
	public class Product
	{
        [SQLite.PrimaryKey]
        public int Id { get; set; }
        public Boolean IsActive { get; set; }
        public int ServerId { get; set; }
        public DateTime Modified { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public string DetailDescription { get; set; }
        public string Image { get; set; }
        public decimal Height { get; set; }
        public decimal Weidth { get; set; }
        public decimal Length { get; set; }
    }
}

