﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TTRBookings.Core.Entities;
using TTRBookings.Core.Interfaces;
using TTRBookings.Web.Helpers;

namespace TTRBookings.Web.ViewComponents
{
    public class HousesViewComponent : ViewComponent
    {
        public List<SelectListItem> Houses { get; } = new List<SelectListItem> { };

        private readonly IRepository repository;
        public HousesViewComponent(IRepository repository)
        {
            this.repository = repository;
        }

        public IViewComponentResult Invoke()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("HouseId")))
            {
                Houses.AddRange(SelectListHelper.PopulateList(
                    repository.List<House>(), e => e.Name
                    ));

                HttpContext.Session.SetString("HouseId", Houses.FirstOrDefault().Value);
            }
            else
            {
                Houses.AddRange(SelectListHelper.PopulateList(
                    repository.List<House>(), e => e.Name,
                    Guid.Parse(HttpContext.Session.GetString("HouseId"))
                    ));
            }

            return View(new SelectedHouse(HttpContext.Session.GetString("HouseId"), Houses));
        }
    }

    public class SelectedHouse
    {
        public List<SelectListItem> Houses { get; } = new List<SelectListItem> { };
        public string HouseId { get; set; }

        public SelectedHouse(string houseId, List<SelectListItem> houses)
        {
            HouseId = houseId;
            Houses = houses;
        }
    }
}