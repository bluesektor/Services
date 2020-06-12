// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System.ComponentModel.DataAnnotations;

namespace GreenWerx.Web.Models
{
    public class TosApiKey
    {
        public bool TOSAgree { get; set; }

        [StringLength(32)]
        public string UserUUID { get; set; }

        public bool WarningUnderstand { get; set; }
        //////public int Id { get; set; }
    }
}