using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmbeddedIronPython.Domain
{
    public class TaskQueueObject
    {
        public FeedingEventType EventType { get; set; }
        public Object Data { get; set; }
    }
}
