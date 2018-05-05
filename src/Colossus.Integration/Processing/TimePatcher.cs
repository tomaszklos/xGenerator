using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Colossus.Web;
using Sitecore.Analytics;
using Sitecore.Analytics.Tracking;
using Sitecore.Sites;
using Sitecore.Diagnostics;

namespace Colossus.Integration.Processing
{
    public class TimePatcher : ISessionPatcher
    {
        public void UpdateSession(Session session, RequestInfo requestInfo)
        {
            Log.Info("START : TimePatcher, UpdateSession", this);

            session.Interaction.StartDateTime = TimeZoneInfo.ConvertTimeToUtc(requestInfo.Visit.Start);
            session.Interaction.EndDateTime = TimeZoneInfo.ConvertTimeToUtc(requestInfo.Visit.End);

            var page = session.Interaction.CurrentPage;

            if (page != null)
            {
                page.DateTime = TimeZoneInfo.ConvertTimeToUtc(requestInfo.Start);
                page.Duration = (int)Math.Round((requestInfo.End - requestInfo.Start).TotalMilliseconds);
            }

            Log.Info("START : TimePatcher, UpdateSession", this);
        }
    }
}
