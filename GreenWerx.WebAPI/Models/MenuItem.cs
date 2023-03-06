// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.Collections.Generic;

namespace GreenWerx.WebAPI.Models
{
    public class MenuItem
    {
        public MenuItem()
        {
            Id = Guid.NewGuid().ToString("N");
            this.items = new List<MenuItem>();
        }
        public string Id { get; set; }
        public string href { get; set; }
        public string icon { get; set; }
        public List<MenuItem> items { get; set; }
        public string label { get; set; }
        public int SortOrder { get; set; }
        public string type { get; set; }
    }
}