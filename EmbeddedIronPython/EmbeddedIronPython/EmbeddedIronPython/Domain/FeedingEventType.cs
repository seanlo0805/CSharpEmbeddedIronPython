using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmbeddedIronPython.Domain
{
    public enum FeedingEventType
    {
        [Description("handling data into python script")]
        OnSingalReceived,

        [Description("timer event for each 1 second")]
        OnTimer1s,
        [Description("timer event for each 10 seconds")]
        OnTimer10s,
        [Description("timer event for eache 60 seconds")]
        OnTimer60s

    }
}
