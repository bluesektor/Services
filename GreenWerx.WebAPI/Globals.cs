// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using GreenWerx.Models.Datasets;
using GreenWerx.Models.General;

namespace GreenWerx.Web
{
    //WARNING: These are not threadsafe, and if your using in a web garden they may fail.
    //These are here for fast access since they are hammered on each request.
    //
    public static class Globals
    {
        public static bool AddRequestPermissions;
        public static WebApplication Application;

        public static string DBConnectionKey;
        public static DataFilter DefaultDataFilter;
        public static int MaxRecordsPerRequest = 500;
        public static StatusMessage Status;
        //todo put this in settings and intialize in WebApplication.cs

        public static void InitializeGlobals()
        {
            if (Globals.Application == null)
                Globals.Application = new WebApplication();

            if (Globals.Status == null)
                Globals.Status = new StatusMessage();

            if (Globals.DefaultDataFilter == null)
            {
                Globals.DefaultDataFilter = new DataFilter()
                {
                    PageResults = true,
                    PageSize = 25,
                    StartIndex = 0,
                    SortBy = "Name",
                    SortDirection = "ASC"
                };
            }
        }
    }
}