using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace DocumentUpload_App.Models
{
    [Table("TA_LOGIN")]  
    public class UserLogin
    {
        [Key]
        public decimal Sno { get; set; }

        public string User_Group { get; set; }

        public string User_Name { get; set; }

        public string Email_Id { get; set; }

        public string Password { get; set; }

        public string Mobile_No { get; set; }

        public DateTime? Created_On { get; set; }

        public string Permission { get; set; }

        public string EMP_ID { get; set; }
    }
}