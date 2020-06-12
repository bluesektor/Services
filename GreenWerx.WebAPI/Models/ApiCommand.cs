// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System.Collections.Generic;

namespace GreenWerx.WebAPI.Models
{
    public class ApiCommand
    {
        public List<KeyValuePair<string, string>> Arguments;

        public ApiCommand()
        {
            this.Arguments = new List<KeyValuePair<string, string>>();
        }

        public string Command { get; set; }
    }
}