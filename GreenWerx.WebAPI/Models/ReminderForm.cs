// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System.ComponentModel.DataAnnotations.Schema;
using GreenWerx.Models.Events;

namespace GreenWerx.Web.Models
{
    public class ReminderForm : Reminder
    {
        [NotMapped]
        public string Captcha { get; set; }
    }
}