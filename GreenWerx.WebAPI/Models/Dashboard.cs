// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GreenWerx.Models.Membership;

namespace GreenWerx.Web.Models
{
    public class Dashboard
    {
        public Dashboard()
        {
            Content = new List<KeyValuePair<string, string>>();
            SideMenuItems = new List<WebAPI.Models.MenuItem>();
            TopMenuItems = new List<WebAPI.Models.MenuItem>();
            Profile = new Profile();
        }

        [NotMapped]
        public List<Role> AccountRoles { get; set; }

        [StringLength(32)]
        public string AccountUUID { get; set; }

        public string Authorization { get; set; }
        public string CartTrackingId { get; set; }

        [NotMapped]
        public List<KeyValuePair<string, string>> Content { get; set; }

        public string Domain { get; set; }
        public bool IsAdmin { get; set; }
        public string Location { get; set; }
        public string LocationType { get; set; }
        public Profile Profile { get; set; }
        public string ReturnUrl { get; set; }
        public double SessionLength { get; set; }
        public string ShoppingCartUUID { get; set; }

        [NotMapped]
        public List<WebAPI.Models.MenuItem> SideMenuItems { get; set; }

        public string Title { get; set; }

        [NotMapped]
        public List<WebAPI.Models.MenuItem> TopMenuItems { get; set; }

        [StringLength(32)]
        public string UserUUID { get; set; }

        public string UserName { get; set; }

        public string View { get; set; }
    }
}