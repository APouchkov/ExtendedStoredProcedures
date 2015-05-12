using System;

namespace PublicDomain.ZoneInfo
{
    public struct OffsetInfo
    {
        private readonly DateTime _cutoverDate;
        private bool _hasRule;
        private TimeSpan _dstOffset;
        private TimeSpan _gmtOffset;

        public OffsetInfo(ZoneRule zone, Rule rule, DateTime cutoverDate)
        {
            _cutoverDate = cutoverDate;
            _gmtOffset = zone.GmtOffset;
            _dstOffset = rule.Save;
            _hasRule = true;
        }

        public OffsetInfo(ZoneRule zone)
        {
            _gmtOffset = zone.GmtOffset;
            _hasRule = false;
            _cutoverDate = DateTime.MinValue;
            _dstOffset = TimeSpan.Zero;
        }

        public DateTime CutoverDate
        {
            get { return _cutoverDate; }
        }

        /// <summary>
        /// Gets Daylight Saving Time offset (if applicable)
        /// </summary>
        public TimeSpan DstOffset
        {
            get { return _dstOffset; }
        }

        /// <summary>
        /// Gets GMT offset.
        /// </summary>
        public TimeSpan GmtOffset
        {
            get { return _gmtOffset; }
        }

        public bool HasRule
        {
            get { return _hasRule; }
        }
    }
}