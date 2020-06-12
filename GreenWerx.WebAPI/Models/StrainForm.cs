// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System.ComponentModel.DataAnnotations.Schema;
using GreenWerx.Models.Plant;

namespace GreenWerx.Web.Models
{
    [Table("Strains")]
    public class StrainForm : Strain
    {
        [NotMapped]
        public string BreederName { get; set; }

        [NotMapped]
        public string Captcha { get; set; }

        [NotMapped]
        public string VarietyName { get; set; }
    }
}