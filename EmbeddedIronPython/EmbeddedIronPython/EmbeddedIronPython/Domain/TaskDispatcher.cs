using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EmbeddedIronPython.Domain
{
    public class TaskMeta
    {
        public BlockingCollection<TaskQueueObject> _queue { get; set; }// thread-safe
        public CancellationTokenSource _cts { get; set; }
        public TaskMeta(BlockingCollection<TaskQueueObject> queue, CancellationTokenSource cts)
        {
            this._queue = queue;
            this._cts = cts;
        }

        public bool isTaskStop()
        {
            return _cts.Token.IsCancellationRequested;
        }
    }
    public class TaskDispatcher
    {
        Task _task = null;
        CancellationTokenSource _cts = null;

        public static TaskDispatcher StartNew(Action<TaskMeta> callback, BlockingCollection<TaskQueueObject> queue)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            TaskMeta meta = new TaskMeta(queue, cts);

            //Task task = Task.Run(() => callback());       => Action callback                  => void callback();
            //Task task = Task.Run(() => callback());       => Action<TaskMeta> callback        => void callback(TaskMeta meta);
            //Task task = Task.Run(() => callback());       => Func<bool> callback              => bool callback();
            //Task task = Task.Run(() => callback(meta));   => Func<TaskMeta, bool> callback    => bool callback(TaskMeta meta);

            Task task = Task.Run(() => callback(meta));

            if (task != null)
                return new TaskDispatcher(task, cts);
            else
                return null;
        }
        private TaskDispatcher(Task task, CancellationTokenSource cts)
        {
            _task = task;
            _cts = cts;
        }
        public void Stop()
        {
            if (_cts != null)
                _cts.Cancel();
        }
    }
}
