using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using TTRBookings.Core.Entities;
using TTRBookings.Core.Interfaces;
using TTRBookings.Web.Helpers;
using TTRBookings.Web.Models;

namespace TTRBookings.Web.Pages.Bookings
{
    public class EditModel : PageModel
    {
        //private readonly ILogger<EditModel> _logger;
        private readonly IRepository repository;
        
        [BindProperty]
        public BookingVM BookingVM { get; set; }

        public Dictionary<string, string> ToastrErrors { get; set; } = new Dictionary<string, string> { };

        public List<SelectListItem> RoomList { get; } = new List<SelectListItem>();
        public List<SelectListItem> RoseList { get; } = new List<SelectListItem>();

        //public EditModel(ILogger<EditModel> logger, IRepository repository)
        //{
        //    _logger = logger;
        //    this.repository = repository;
        //}

        public EditModel(IRepository repository)
        {
            this.repository = repository;
        }

        public void OnGet(Guid id)
        {
            var booking = repository.ReadEntryWithIncludes<Booking>(id, _ => _.Room, _ => _.Rose, _ => _.TimeSlot, _ => _.Tier);

            RoomList.AddRange(SelectListHelper.PopulateList<Room>(
                repository.List<Room>(_ => _.HouseId == Guid.Parse(HttpContext.Session.GetString("HouseId"))),
                e=>e.Name,
                booking.Room.Id
            ));

            RoseList.AddRange(SelectListHelper.PopulateList<Rose>(
                repository.List<Rose>(_ => _.HouseId == Guid.Parse(HttpContext.Session.GetString("HouseId"))), 
                e=>e.Name, 
                booking.Rose.Id
            ));

            //convert booking to bookingvm here;
            BookingVM = BookingVM.Create(booking);
        }

        public IActionResult OnPost()
        {
            //place backend validation logic here.
            CheckAgainstBusinessRules();

            if (!ModelState.IsValid)
            {
                //Load Lists before returning the Page
                RoomList.AddRange(SelectListHelper.PopulateList<Room>(
                    repository.List<Room>(_ => _.HouseId == Guid.Parse(HttpContext.Session.GetString("HouseId"))),
                    e => e.Name,
                    BookingVM.Room.Id
                ));

                RoseList.AddRange(SelectListHelper.PopulateList<Rose>(
                    repository.List<Rose>(_ => _.HouseId == Guid.Parse(HttpContext.Session.GetString("HouseId"))),
                    e => e.Name,
                    BookingVM.Rose.Id
                ));

                return Page();
            }

            //retrieve original from database.
            Booking booking = repository.ReadEntryWithIncludes<Booking>(BookingVM.Id, _ => _.Room, _ => _.Rose, _ => _.TimeSlot, _ => _.Tier);

            //assign bookingVM values to booking

            //TODO - create method to update booking given parameters
            booking.Room = repository.ReadEntry<Room>(BookingVM.Room.Id);
            booking.Rose = repository.ReadEntry<Rose>(BookingVM.Rose.Id);
            booking.Tier.Rate = BookingVM.Tier.Rate;
            booking.TimeSlot.Start = BookingVM.TimeSlot.Start;
            booking.TimeSlot.End = BookingVM.TimeSlot.End;

            //store in database
            repository.UpdateEntry(booking);

            //return/redirect user to somewhere
            return RedirectToPage("/Bookings/Edit", new { booking.Id });
        }

        private void CheckAgainstBusinessRules()
        {
            //Bookings can't be made in the past.
            if (BookingVM.TimeSlot.Start < DateTime.Now)
            {
                ModelState.AddModelError("StartDateBeforeCurrentDate", "[StartDate]: Cannot be in the past.");
                ToastrErrors.Add("Invalid Start Date", "Start Date can't be in the past.");
            }
            //TimeSlot Start can't be after TimeSlot End
            if (BookingVM.TimeSlot.Start > BookingVM.TimeSlot.End)
            {
                ModelState.AddModelError("EndDateBeforeStartDate", "[EndDate]: Cannot be before [StartDate]");
                ToastrErrors.Add("Invalid End Date", "End Date can't be before Start Date.");
            }

            IList<Booking> existing = repository.ListWithIncludes<Booking>(
                //the filter
                booking => !booking.IsDeleted
                && booking.HouseId == Guid.Parse(HttpContext.Session.GetString("HouseId"))
                && booking.Room.Id == BookingVM.Room.Id
                && booking.Rose.Id == BookingVM.Rose.Id
                && booking.Id != BookingVM.Id
                ,
                //the includes
                _ => _.Room, _ => _.Rose, _ => _.TimeSlot);

            if (existing.Any()) // input data from database, to check against input from frontend
            {
                //Is Start time of NEW booking within timeslot of EXISTING booking?
                if (existing.Where(booking =>
                    BookingVM.TimeSlot.Start > booking.TimeSlot.Start
                    && BookingVM.TimeSlot.Start < booking.TimeSlot.End).Any())//check case X of overlap schema.
                {
                    ModelState.AddModelError("NewTimeslotStartContainedWithinExistingTimeslot", "[StartDate]: Cannot be within timeslot of existing booking.");
                    ToastrErrors.Add("Start Time Scheduling Issue", "The Start Time overlaps with an existing booking.");
                }

                //Is End time of NEW booking within timeslot of EXISTING booking?
                if (existing.Where(booking =>
                    BookingVM.TimeSlot.End > booking.TimeSlot.Start
                    && BookingVM.TimeSlot.End < booking.TimeSlot.End).Any())//check case X of overlap schema.
                {
                    ModelState.AddModelError("NewTimeslotEndContainedWithinExistingTimeslot", "[EndDate]: Cannot be within timeslot of existing booking.");
                    ToastrErrors.Add("End Time Scheduling Issue", "The End Time overlaps with an existing booking.");
                }

                //Is Start time & End Time of EXISTING booking contained within timeslot of NEW booking?
                if (existing.Where(booking =>
                    booking.TimeSlot.Start > BookingVM.TimeSlot.Start
                    && booking.TimeSlot.Start < BookingVM.TimeSlot.End
                    && booking.TimeSlot.End > BookingVM.TimeSlot.Start
                    && booking.TimeSlot.End < BookingVM.TimeSlot.End).Any())//check case X of overlap schema.
                {
                    ModelState.AddModelError("ExistingTimeslotStartAndEndContainedWithinNewTimeslot", "[ExistingStartDate] and [ExistingEndDate]: Cannot be within timeslot of new booking.");
                    ToastrErrors.Add("Existing Booking Overlap", "A booking that uses that timeslot already exists.");
                }

                //Is Start time & End Time of NEW booking within timeslot of EXISTING booking?
                if (existing.Where(booking =>
                    BookingVM.TimeSlot.Start > booking.TimeSlot.Start
                    && BookingVM.TimeSlot.Start < booking.TimeSlot.End
                    && BookingVM.TimeSlot.End > booking.TimeSlot.Start
                    && BookingVM.TimeSlot.End < booking.TimeSlot.End).Any())//check case X of overlap schema.
                {
                    ModelState.AddModelError("NewTimeslotStartAndEndContainedWithinExistingTimeslot", "[StartDate] and [EndDate]: Cannot be within timeslot of existing booking.");
                    ModelState.Remove("NewTimeslotStartContainedWithinExistingTimeslot");
                    ModelState.Remove("NewTimeslotEndContainedWithinExistingTimeslot");

                    ToastrErrors.Add("New Booking Overlap", "The new booking overlaps an existing booking.");
                    ToastrErrors.Remove("Start Time Scheduling Issue");
                    ToastrErrors.Remove("End Time Scheduling Issue");
                }

                //Is Start time of NEW booking within timeslot of EXISTING booking X, and End time of NEW booking within timeslot of EXISTING booking Y?
                var condition1 = existing.Where(booking =>
                    BookingVM.TimeSlot.Start > booking.TimeSlot.Start
                    && BookingVM.TimeSlot.Start < booking.TimeSlot.End);
                if (condition1.Any())
                {
                    Guid idCondition1 = condition1.FirstOrDefault().Id;
                    var condition2 = existing.Where(booking =>
                    BookingVM.TimeSlot.End > booking.TimeSlot.Start
                    && BookingVM.TimeSlot.End < booking.TimeSlot.End
                    && booking.Id != idCondition1).Any();

                    if (condition2)//check case X of overlap schema.
                    {
                        ModelState.AddModelError("NewBookingOverlapsWithinMultipleExistingTimeslots", "[StartDate and EndDate]: Cannot overlap with existing bookings.");
                        ToastrErrors.Add("New Booking Overlaps Multiple Bookings", "The new booking overlaps at least two existing booking.");
                    }
                }
            }
        }
    }
}
