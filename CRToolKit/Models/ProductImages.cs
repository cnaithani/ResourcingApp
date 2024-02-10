using System;
namespace CRToolKit.Models
{
	public class ProductImages
	{
        [SQLite.PrimaryKey]
        public int Id { get; set; }
        public Boolean IsActive { get; set; }
        public int ServerId { get; set; }
        public DateTime Modified { get; set; }
        public int ProductId { get; set; }
        public string Path { get; set; }
    }
}

