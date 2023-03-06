// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using GreenWerx.Models;
using System.Collections.Generic;

namespace GreenWerx.Web.Models
{
    public class ToolsDashboard
    {
        public List<Node> Backups { get; set; }

        public string DefaultDatabase { get; set; }

        public List<string> ImportFiles { get; set; }
    }
}