using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocumentUpload_App.Models
{
    [Table("Tab_Documents")] // apni exact table name daalo
    public class Document
    {
        [Key]
        public int EnteryNo { get; set; }
        public DateTime? EntryDate { get; set; }
        public string EntryBy { get; set; }
        public string MasterDocumentName { get; set; }
        public string Topic { get; set; }
        public string DocumentType { get; set; }
        public DateTime? LastUpdate { get; set; }
        public string UpdateBy { get; set; }
        public string Path { get; set; }
        public string DocumentPath { get; set; }
        public int? No_Of_visitors { get; set; }
    }
}