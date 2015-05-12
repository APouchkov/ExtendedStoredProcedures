/*
 * ZoneInfo .NET API
 * Developed by Mark Rodrigues
 * Published under the Microsoft Public License (Ms-PL)
 * For the latest version and to log issues please go to:
 * http://www.codeplex.com/zoneinfo
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PublicDomain.ZoneInfo
{
    /// <summary>
    /// A class representing a given timezone.
    /// This is the major entry point into accessing the key
    /// timezone functions.
    /// </summary>
    /// <remarks>
    /// A zone object can only be created using the Parse method.
    /// </remarks>
    public class Zone
    {
        #region Private Constructor

        private Zone()
        {
            _zoneRules = new List<ZoneRule>();
        }

        #endregion // Private Constructor

        #region Public Members

        /// <summary>
        /// The name of the zone
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }
        private string _name;

        /// <summary>
        /// The list of rules for the current zone
        /// </summary>
        public List<ZoneRule> ZoneRules
        {
            get
            {
                return _zoneRules;
            }
        }
        private List<ZoneRule> _zoneRules;

        /// <summary>
        /// The time NOW in this zone
        /// </summary>
        public DateTime Now
        {
            get
            {
                // Just convert the utcnow to local time
                return ConvertToLocal(DateTime.UtcNow);
            }
        }

        #endregion // Public Members

        #region Public Static Methods

        /// <summary>
        /// Creates a zone based on a text string
        /// </summary>
        /// <param name="line">A given rule line from the zone file</param>
        /// <param name="defaultZone">The default zone to use if not specified in the line</param>
        /// <remarks>
        /// The format of each line is:
        /// Zone	NAME		GMTOFF	RULES	FORMAT	[UNTIL]
        /// 
        /// Zones can appear in sequential lines in the file
        /// and in this case the zone information from the previous line
        /// is carried on.
        /// 
        /// Note: The format of the file seems pretty inconsistent
        /// so this function is not as elegant as it could be.
        /// </remarks>
        public static Zone Parse(string line, Zone defaultZone)
        {
          // Create the zone we will be returning
          Zone      zone = null;
          DateTime  Since;

          // Split up the line
          string[] arr       = Regex.Replace(line, "\\s+", "\t").Split('\t');
          int      arrOffset = 0;

          try
          {
            if ((arr.Length < 4) || ((defaultZone == null) && (arr[0] != "Zone")))
              throw new Exception("Неверный формат строки(1)");

            if (arr[0] == "Zone")
            {
              // The first token of the string is not empty.
              // Therefore it must be a new zone
              // Create a new zone
              zone = new Zone();
              zone.Name = arr[1];

              Since = DateTime.MinValue;
              arrOffset = 2;
            }
            else
            {
              if (arr[0].Length != 0)
                throw new Exception("Неверный формат строки(2)");

              // The first token of the string is empty
              // Therefore this is another rule of the last zone
              zone  = defaultZone;
              Since = defaultZone.ZoneRules[defaultZone.ZoneRules.Count - 1].Until;
              arrOffset = 1;
            }

            // Create a new ZoneRule for storing these details
            ZoneRule zoneRule = new ZoneRule();
            zone.ZoneRules.Add(zoneRule);

            zoneRule.ZoneName   = zone.Name;
            zoneRule.Since      = Since;
            zoneRule.GmtOffset  = Database.ConvertToTimeSpan(arr[arrOffset]);
            zoneRule.RuleName   = arr[arrOffset + 1];
            zoneRule.Format     = arr[arrOffset + 2];

            arrOffset += 3;

            // Handle until date
            if (arrOffset + 1 > arr.Length)
            {
                // There are no more values in the array.
                // Let's set the Until date as forever
                zoneRule.Until = DateTime.MaxValue;
            }
            else
            {
                // There is a Until value set
                int year  = Convert.ToInt32(arr[arrOffset++]);
                int month = 1;
                int day   = 1;
                int hour  = 0;
                int min   = 0;
                int sec   = 0;

                // Parse the month
                if (arrOffset + 1 < arr.Length)
                {
                  month = Database.ConvertToMonth(arr[arrOffset++]);
                  if (arrOffset + 1 < arr.Length)
                  {
                    if (!Int32.TryParse(arr[arrOffset++], out day))
                    {
                      day = Database.ConvertToDateTime(arr[arrOffset - 1], year, month).Day;
                    }

                    if (arrOffset + 1 < arr.Length)
                    {
                      TimeSpan ts = Database.ConvertToTimeSpan(arr[arrOffset]);
                      hour = ts.Hours;
                      min  = ts.Minutes;
                      sec  = ts.Seconds;
                    }
                  }
                }

                zoneRule.Until = new DateTime(year, month, day, hour, min, sec, DateTimeKind.Local);
              }
            }
            catch (Exception E)
            {
              throw new Exception(String.Format("Ошибка разбора зоны '{0}': {1}", line, E.Message));
            }
            
            return zone;
        }

        #endregion // Public Static Methods

        #region Public Methods

        /// <summary>
        /// Returns the format string for the zone for the given date
        /// </summary>
        /// <param name="dt">A local date time</param>
        /// <returns>A format string associated with the date in the given zone.</returns>
        /// <example>In Australia/Melbourne the format strings are
        /// AEST or AEDT if in daylight savings</example>
        public string GetFormat(DateTime dt)
        {
            // Get the Zone Rule for the datetime
            ZoneRule zrule = GetZoneRule(dt);

            // Check to see if there are any rules for this zone
            if (Database.Rules.ContainsKey(zrule.RuleName))
            {
                // There are matching rules.
                // Now get the exact rule
                DateTime cutoverDt = DateTime.MinValue;
                Rule rule = GetRule(dt, Database.Rules[zrule.RuleName], out cutoverDt);

                if (rule != null)
                {
                    // Modify the format with the rule letter
                    string letter = rule.Letters;
                    if (letter == "-")
                    {
                        letter = "";
                    }
                    if (zrule.Format.Contains("%s"))
                        return zrule.Format.Replace("%s", letter);
                    if (zrule.Format.Contains("/"))
                        return zrule.Format.Split('/')[letter == string.Empty ? 0 : 1];
                    return zrule.Format;
                }
            }

            // We couldn't find an approprirate rule.
            // Just return the format of the zone
            return zrule.Format;
        }

        /// <summary>
        /// Returns the utc offset for the datetime specified (no dst offset taken into account)
        /// </summary>
        /// <param name="dt">The local time time</param>
        /// <returns>The utc for the local datetime in the current zone</returns>
        public TimeSpan GetUtcOffset(DateTime dt)
        {
            // Get the Zone Rule for the datetime
            ZoneRule zrule = GetZoneRule(dt);

            // Just return the standard offset for the zone
            return zrule.GmtOffset;
        }

        /// <summary>
        /// Returns the utc offset + dst offset (if any) for the datetime specified
        /// </summary>
        /// <param name="dt">The time - if Utc it will be converted to Local first to check for any DST rules</param>
        /// <returns>Offset ingo with the zone, rule and cutover date</returns>
        public OffsetInfo GetOffsets(DateTime dt)
        {
            // Get the Zone Rule for the datetime
            ZoneRule zrule = GetZoneRule(dt);

            //DST rules are defined using local time, so need to add GmtOffset first
            if (dt.Kind == DateTimeKind.Utc)
            {
                dt = dt.Add(zrule.GmtOffset);
            }

            return GetDstOffset(dt, zrule);
        }

        /// <summary>
        /// Converts the current datetime to UTC
        /// </summary>
        /// <param name="dt">A datetime to convert.
        /// If it is not of time DateTimeKind.Utc then it is assumed
        /// to be of type DateTimeKind.Local</param>
        /// <returns>A datetime of type DateTimeKind.Utc</returns>
        public DateTime ConvertToUtc(DateTime dt)
        {
            // The the kind is UTC already then use this.
            // Else just assume it is Local even if not specified
            if (dt.Kind == DateTimeKind.Utc)
            {
                return dt;
            }

            OffsetInfo offset = GetOffsets(dt);

            if (offset.HasRule)
            {
                //check if transition period
                if (offset.DstOffset != TimeSpan.Zero && dt >= offset.CutoverDate && dt < offset.CutoverDate.Add(offset.DstOffset))
                {
                    //switching the rule on and is a transition - e.g. 2-3pm is the same as 3-4 pm (moving clock 1h forward)
                    //don't subtract the offset
                    return new DateTime(dt.Subtract(offset.GmtOffset).Ticks, DateTimeKind.Utc);
                }
                else if (offset.DstOffset == TimeSpan.Zero && dt >= offset.CutoverDate && dt < offset.CutoverDate.Add(offset.DstOffset))
                {
                    //switching the rule off and is a transition - e.g. 1-2pm would be ran twice (moving clock 1h backward)
                    //don't subtract the offset
                    return new DateTime(dt.Subtract(offset.GmtOffset).Ticks, DateTimeKind.Utc);
                }
                //subtract DST offset
                return new DateTime(dt.Subtract(offset.GmtOffset).Subtract(offset.DstOffset).Ticks, DateTimeKind.Utc);
            }

            // Now just subtract the standard offset
            return new DateTime((dt - offset.GmtOffset).Ticks, DateTimeKind.Utc);
        }

        /// <summary>
        /// Converts the current datetime to local
        /// </summary>
        /// <param name="dt">A datetime to convert.
        /// If it is not of time DateTimeKind.Local then it is assumed
        /// to be of type DateTimeKind.Utc</param>
        /// <returns>A datetime of type DateTimeKind.Local</returns>
        public DateTime ConvertToLocal(DateTime dt)
        {
            // The the kind is Local already then use this.
            // Else just assume it is UTC even if not specified
            if (dt.Kind == DateTimeKind.Local)
            {
                return dt;
            }

            dt = new DateTime(dt.Add(GetUtcOffset(dt)).Ticks, DateTimeKind.Local);

            OffsetInfo offset = GetOffsets(dt);

            if (offset.HasRule)
            {
                //check if transition period
                if (offset.DstOffset == TimeSpan.Zero && dt >= offset.CutoverDate && dt < offset.CutoverDate.Add(offset.DstOffset))
                {
                    //switching the rule off and is a transition - e.g. 1-2pm would be ran twice (moving clock 1h backward)
                    //don't add the offset
                    return new DateTime(dt.Ticks, DateTimeKind.Local);
                }
                //add DST offset
                return new DateTime(dt.Add(offset.DstOffset).Ticks, DateTimeKind.Local);
            }

            // Now just add the standard offset
            return new DateTime(dt.Ticks, DateTimeKind.Local);
        }

        /// <summary>
        /// Returns the dates in which the day light savings cutover
        /// during a given date window.
        /// </summary>
        /// <param name="from">The from date which is assumed to be
        /// of DateTimeKind.Local</param>
        /// <param name="to">The end date which is assumed to be
        /// of DateTimeKind.Local</param>
        /// <returns>An ordered array of local datetimes where day light
        /// savings cut-over</returns>

      public struct CutoverWindow
      {
        public DateTime DateTime;
        public TimeSpan GmtOffset;
      }

      public List<CutoverWindow> GetCutoverWindows(DateTime from, DateTime to)
        {
            CutoverWindow date;
            List<CutoverWindow> dates = new List<CutoverWindow>();

            // Keep looking for the last date from the to date and keep
            // working backwards
            DateTime tmpDt = to;
            while (tmpDt >= from)
            {
                // The cutover time of this rule
                //DateTime cutoverDt = DateTime.MinValue;
                ZoneRule zrule = GetZoneRule(tmpDt);
                Rule rule;
                if (!Database.Rules.ContainsKey(zrule.RuleName)
                    || (rule = GetRule(tmpDt, Database.Rules[zrule.RuleName], out date.DateTime)) == null)
                {
                    // We couldn't find a rule
                    // break;
                  date.DateTime = zrule.Since;
                  date.GmtOffset = zrule.GmtOffset;
                }
                else
                  date.GmtOffset = zrule.GmtOffset + rule.Save;

                if (date.DateTime < from)
                {
                  date.DateTime = from;
                }

                dates.Insert(0, date);
                tmpDt = date.DateTime.AddMinutes(-1);
            }

            return dates;
        }

        #endregion // Public Methods

        #region Private Methods

        private ZoneRule GetZoneRule(DateTime dt)
        {
            foreach (ZoneRule zrule in ZoneRules)
            {
                // If the datetime is before the rule expires then use this one
                if (dt <= zrule.Until)
                {
                    return zrule;
                }
            }

            throw new ArgumentException("No rule found for given date", dt.ToString());
        }

        private Rule GetRule(DateTime dt, List<Rule> rules, out DateTime cutoverDt)
        {
            // Keep a handle on the best match
            Rule bestRule = null;
            cutoverDt = DateTime.MinValue;

            // Iterate through all the rules
            foreach (Rule rule in rules)
            {
                if (dt.Year >= rule.From && dt.Year <= rule.To + 1)
                {
                    // This rule may be in the range
                    DateTime newDt = DateTime.MinValue;
                    
                    // Try the year before
                    if (rule.TryGetCutoverDateTime(dt.Year - 1, out newDt) && newDt > cutoverDt && newDt <= dt) //rule can take effect before or on the date
                    {
                        // We have a new winner
                        cutoverDt = newDt;
                        bestRule = rule;
                    }

                    // Try the current year
                    if (rule.TryGetCutoverDateTime(dt.Year, out newDt) && newDt > cutoverDt && newDt <= dt) //rule can take effect before or on the date
                    {
                        // We have a new winner
                        cutoverDt = newDt;
                        bestRule = rule;
                    }
                }
            }

            // Return the right one
            return bestRule;
        }

        private OffsetInfo GetDstOffset(DateTime dt, ZoneRule zone)
        {
            // Keep a handle on the best match
            Rule bestRule = null;
            DateTime cutoverDt = DateTime.MinValue;

            // Check to see if there are any rules for this zone
            if (!Database.Rules.ContainsKey(zone.RuleName))
                //no rule
                return new OffsetInfo(zone);

            // There are matching rules.
            // Now get the exact rule

            // Iterate through all the rules
            foreach (Rule rule in Database.Rules[zone.RuleName])
            {
                if (dt.Year >= rule.From && dt.Year <= rule.To + 1)
                {
                    // This rule may be in the range
                    DateTime newDt = DateTime.MinValue;

                    // Try the year before
                    if (rule.TryGetCutoverDateTime(dt.Year - 1, out newDt) && newDt > cutoverDt && newDt <= dt) //rule can take effect before or on the date
                    {
                        // We have a new winner
                        cutoverDt = newDt;
                        bestRule = rule;
                    }

                    // Try the current year
                    if (rule.TryGetCutoverDateTime(dt.Year, out newDt) && newDt > cutoverDt && newDt <= dt) //rule can take effect before or on the date
                    {
                        // We have a new winner
                        cutoverDt = newDt;
                        bestRule = rule;
                    }
                }
            }

            // Return the right one
            if (bestRule == null)
                return new OffsetInfo(zone);

            return new OffsetInfo(zone, bestRule, cutoverDt);
        }

        #endregion // Private Methods
    }
}
