using EmbeddedIronPython.Domain;
using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using System;
using System.Collections.Concurrent;
using System.Timers;

namespace EmbeddedIronPython
{
    public class PythonEngineImpl
    {
        #region member data
        /// <summary>
        /// script engine
        /// </summary>
        private ScriptEngine _scriptEngine = Python.CreateEngine();
        /// <summary>
        /// script scope: assign variable and object
        /// </summary>
        private ScriptScope _scriptScope = null;

        /// <summary>
        /// compiled source code
        /// </summary>
        private CompiledCode _compiled = null;

        /// <summary>
        /// Python source code
        /// </summary>
        private string _sourceCode = "";

        /// <summary>
        /// queue for tasks
        /// </summary>
        BlockingCollection<TaskQueueObject> _queue;

        /// <summary>
        /// thread dispatch
        /// </summary>
        private TaskDispatcher _taskDispather;

        /// <summary>
        /// queuing task count
        /// </summary>
        private int _lastQueuedCount = 0;


        private Timer _timer1s = null;
        private Timer _timer10s = null;
        private Timer _timer60s = null;
        #endregion

        #region property
        public string SourceCode
        {
            get { return _sourceCode; }
        }

        protected delegate void delegate_OnTimer();


        /// <summary>
        /// evens gose into python engine
        /// </summary>
        protected delegate_OnTimer OnTimer1s { get; set; }
        protected delegate_OnTimer OnTimer10s { get; set; }
        protected delegate_OnTimer OnTimer60s { get; set; }

        /// <summary>
        /// events pump out from python or PScript
        /// </summary>
        public delegate void delegate_PythonEvent(object content, string msg);
        public delegate_PythonEvent OnPythonEvent { get; set; } = (object content, string msg) => { };

        /// <summary>
        /// specified task event
        /// </summary>
        /// <param name="quote"></param>
        protected delegate void delegate_OnSingalReceived(object signal);

        protected delegate_OnSingalReceived OnSingalReceived { get; set; }
        #endregion


        #region member function

        public void PushMsg(string msg)
        {
            FeedData(FeedingEventType.OnSingalReceived, new TaskQueueObject() { EventType = FeedingEventType.OnSingalReceived, Data = msg });
        }

        /// <summary>
        /// 
        /// </summary>
        public void Start(string source)
        {
            _sourceCode = source;
            if (!InitExecute())
            {
                OnPythonEvent(null, "FeedData Init fail");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Stop()
        {
            PreStop();
            if (_taskDispather != null)
            {
                _taskDispather.Stop();
                _taskDispather = null;
            }
        }

        public PythonEngineImpl()
        {
            _queue = new BlockingCollection<TaskQueueObject>();
            _taskDispather = TaskDispatcher.StartNew(Run, _queue);
        }

        protected bool InitExecute()
        {
            PreStart();

            return CompileExecute();
        }

        /// <summary>
        /// task handler function
        /// </summary>
        /// <param name="obj"></param>
        protected void DispatchData(TaskQueueObject obj)
        {
            if (obj == null)
                return;
            if (FeedingEventType.OnSingalReceived.Equals(obj.EventType) && OnSingalReceived != null)
                OnSingalReceived(obj.Data);
            else if (FeedingEventType.OnTimer1s.Equals(obj.EventType) && OnTimer1s != null)
                OnTimer1s();
            else if (FeedingEventType.OnTimer10s.Equals(obj.EventType) && OnTimer10s != null)
                OnTimer10s();
            else if (FeedingEventType.OnTimer60s.Equals(obj.EventType) && OnTimer60s != null)
                OnTimer60s();

            //unhandle

        }

        /// <summary>
        /// compile python source code and execute it
        /// </summary>
        /// <returns></returns>
        protected bool CompileExecute()
        {
            bool result = false;
            try
            {
                _scriptScope = _scriptEngine.CreateScope();
                ScriptSource source = _scriptEngine.CreateScriptSourceFromString(SourceCode, SourceCodeKind.Statements);
                BindingParameter(_scriptScope);

                _compiled = source.Compile();
                _compiled.Execute(_scriptScope);

                BindingInputEvents(_scriptScope);

                result = true;
            }
            catch (Exception e)
            {
                OnPythonEvent(null, "CompileExecute Exception:" + e.StackTrace);
            }
            return result;
        }

        /// <summary>
        /// stop timer
        /// </summary>
        /// <param name="timer"></param>
        /// <returns></returns>
        protected Timer StopTimer(Timer timer)
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Close();
                timer = null;
            }
            return timer;
        }

        /// <summary>
        /// start timer
        /// </summary>
        /// <param name="interval"></param>
        /// <param name="eventType"></param>
        /// <returns></returns>
        protected Timer StartTimer(double interval, FeedingEventType eventType)
        {
            Timer timer = new System.Timers.Timer();
            timer.Interval = interval;

            timer.Elapsed += (sender, e) => FeedTimer(sender, e, eventType);

            timer.AutoReset = true;

            timer.Enabled = true;
            timer.Start();

            return timer;
        }

        protected void FeedTimer(object sender, System.Timers.ElapsedEventArgs args, FeedingEventType eventType)
        {
            FeedData(eventType, new TaskQueueObject() { EventType = eventType, Data = null });
        }


        /// <summary>
        /// feeding data into queue
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        protected bool FeedData(FeedingEventType eventType, TaskQueueObject data)
        {
            bool result = false;

            if (_queue == null)
            {
                OnPythonEvent(null, "FeedData Queue is null");
            }
            else
            {
                try
                {
                    if (!_queue.TryAdd(data, 50))
                    {
                        OnPythonEvent(null, "FeedData TryAdd Queue Fail");
                        result = false;
                    }
                    else
                    {
                        result = true;

                    }
                }
                catch (InvalidOperationException e) //add fail
                {
                    OnPythonEvent(null, "FeedData TryAdd Queue InvalidOperationException:" + e.StackTrace);
                }
                catch (Exception e)
                {
                    OnPythonEvent(null, "FeedData Exception:" + e.StackTrace);
                }
            }
            return result;

        }

        /// <summary>
        /// 
        /// </summary>
        protected void PreStart()
        {
            _scriptEngine = Python.CreateEngine();

            _timer1s = StartTimer(1000, FeedingEventType.OnTimer1s);
            _timer10s = StartTimer(10000, FeedingEventType.OnTimer10s);
            _timer60s = StartTimer(60000, FeedingEventType.OnTimer60s);
        }


        /// <summary>
        /// 
        /// </summary>
        protected void PreStop()
        {

            _timer1s = StopTimer(_timer1s);
            _timer10s = StopTimer(_timer10s);
            _timer60s = StopTimer(_timer60s);

            if (_scriptEngine != null)
                _scriptEngine = null;
        }

        /// <summary>
        /// binding object
        /// </summary>
        /// <param name="scriptScope"></param>
        protected void BindingParameter(ScriptScope scriptScope)
        {
            scriptScope.SetVariable("ThisExecuter", this);


            //OutputEvent
            scriptScope.SetVariable("OnPythonEvent", this.OnPythonEvent);

        }

        /// <summary>
        /// binding call back function for python
        /// </summary>
        /// <param name="scriptScope"></param>
        protected void BindingInputEvents(ScriptScope scriptScope)
        {
            try
            {
                OnTimer1s += scriptScope.GetVariable<delegate_OnTimer>(FeedingEventType.OnTimer1s.ToString());
            }
            catch
            {
                OnTimer1s = null;
            }

            try
            {
                OnTimer10s += scriptScope.GetVariable<delegate_OnTimer>(FeedingEventType.OnTimer10s.ToString());
            }
            catch
            {
                OnTimer10s = null;
            }

            try
            {
                OnTimer60s += scriptScope.GetVariable<delegate_OnTimer>(FeedingEventType.OnTimer60s.ToString());
            }
            catch
            {
                OnTimer60s = null;
            }


            try
            {
                OnSingalReceived += scriptScope.GetVariable<delegate_OnSingalReceived>(FeedingEventType.OnSingalReceived.ToString());
            }
            catch
            {
                OnSingalReceived = null;
            }

        }
        /// <summary>
        /// thread function
        /// </summary>
        /// <param name="meta"></param>
        protected void Run(TaskMeta meta)
        {
            if (meta == null || meta._queue == null)
            {
                OnPythonEvent(null, "Task Meta or Queue is null, could not start Task");
                return;
            }

            while (!meta.isTaskStop())
            {
                try
                {
                    if (!meta._queue.IsCompleted)
                    {
                        TaskQueueObject obj = null;
                        if (!meta._queue.TryTake(out obj, 50))
                        {
                            //queue is empty
                        }
                        else
                        {
                            DispatchData(obj);

                        }

                        _lastQueuedCount = meta._queue.Count;

                    }
                    else
                    {
                        OnPythonEvent(null, "Queue already completed");
                        break;
                    }
                }
                catch (InvalidOperationException e) ///operating queue exception
                {
                    OnPythonEvent(null, "Task TryTake Queue InvalidOperationException:" + e.StackTrace);
                }
                catch (Exception e)
                {
                    OnPythonEvent(null, "Task Exception:" + e.StackTrace);
                }
                finally
                {

                    if (meta != null && meta._queue != null)
                        _lastQueuedCount = meta._queue.Count;
                }
            }
            OnPythonEvent(null, "Task done");

        }
        #endregion
    }
}
