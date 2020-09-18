﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventKit;

namespace Xamarin.Essentials
{
    public static partial class Calendars
    {
        static Task<IEnumerable<Calendar>> PlatformGetCalendarsAsync()
        {
            var calendars = CalendarRequest.Instance.Calendars;

            return Task.FromResult<IEnumerable<Calendar>>(ToCalendars(calendars).ToList());
        }

        static Task<Calendar> PlatformGetCalendarAsync(string calendarId)
        {
            var calendars = CalendarRequest.Instance.Calendars;

            var calendar = calendars.FirstOrDefault(c => c.CalendarIdentifier == calendarId);
            if (calendar == null)
                throw InvalidCalendar(calendarId);

            return Task.FromResult(ToCalendar(calendar));
        }

        static Task<IEnumerable<CalendarEvent>> PlatformGetEventsAsync(string calendarId = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            var startDateToConvert = startDate ?? DateTimeOffset.Now.Add(defaultStartTimeFromNow);
            var endDateToConvert = endDate ?? startDateToConvert.Add(defaultEndTimeFromStartTime); // NOTE: 4 years is the maximum period that a iOS calendar events can search
            var sDate = startDateToConvert.ToNSDate();
            var eDate = endDateToConvert.ToNSDate();

            EKCalendar[] calendars = null;
            if (!string.IsNullOrEmpty(calendarId))
            {
                var calendar = calendars.FirstOrDefault(c => c.CalendarIdentifier == calendarId);
                if (calendar == null)
                    throw InvalidCalendar(calendarId);

                calendars = new[] { calendar };
            }

            var query = CalendarRequest.Instance.PredicateForEvents(sDate, eDate, calendars);
            var events = CalendarRequest.Instance.EventsMatching(query);

            return Task.FromResult<IEnumerable<CalendarEvent>>(ToEvents(events.OrderBy(e => e.StartDate)).ToList());
        }

        static Task<CalendarEvent> PlatformGetEventAsync(string eventId)
        {
            if (!(CalendarRequest.Instance.GetCalendarItem(eventId) is EKEvent calendarEvent))
                throw InvalidEvent(eventId);

            return Task.FromResult(ToEvent(calendarEvent));
        }

        static IEnumerable<Calendar> ToCalendars(IEnumerable<EKCalendar> native)
        {
            foreach (var calendar in native)
            {
                yield return ToCalendar(calendar);
            }
        }

        static Calendar ToCalendar(EKCalendar calendar) =>
            new Calendar
            {
                Id = calendar.CalendarIdentifier,
                Name = calendar.Title
            };

        static IEnumerable<CalendarEvent> ToEvents(IEnumerable<EKEvent> native)
        {
            foreach (var e in native)
            {
                yield return ToEvent(e);
            }
        }

        static CalendarEvent ToEvent(EKEvent native) =>
            new CalendarEvent
            {
                Id = native.CalendarItemIdentifier,
                CalendarId = native.Calendar.CalendarIdentifier,
                Title = native.Title,
                Description = native.Notes,
                Location = native.Location,
                AllDay = native.AllDay,
                StartDate = native.StartDate.ToDateTimeOffset(),
                EndDate = native.EndDate.ToDateTimeOffset(),
                Attendees = native.Attendees != null
                    ? ToAttendees(native.Attendees).ToList()
                    : new List<CalendarEventAttendee>()
            };

        static IEnumerable<CalendarEventAttendee> ToAttendees(IEnumerable<EKParticipant> inviteList)
        {
            foreach (var attendee in inviteList)
            {
                yield return new CalendarEventAttendee
                {
                    Name = attendee.Name,
                    Email = attendee.Name
                };
            }
        }
    }
}
