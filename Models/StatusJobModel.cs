using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace iBusinessManagement.Models
{
    public class StatusJobModel
    {
        [Display(Name = "Status")]
        public int TaskId { get; set; }

        public string Status { get; set; }

        public IEnumerable<SelectListItem> Statuses { get; set; }
    }
}