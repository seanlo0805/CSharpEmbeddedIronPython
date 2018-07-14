using System;

namespace EmbeddedIronPython
{
    class PythonSampleConsole
    {
        string[] _sourceCodeLines =
        {
            @"def OnTimer1s():",
            @"   OnPythonEvent(None, 'timer1s')",
            @"def OnTimer10s():",
            @"   print 'OnTimer10s'",
            @"def OnTimer60s():",
            @"   print 'OnTimer60s'",
            @"def OnSingalReceived(msg):",
            @"   ThisExecuter.OnPythonEvent(None, 'python received:'+msg)",
            @"",
        };

        private string _sourceCode;

        PythonEngineImpl _engine;

        private static void Main(string[] args)
        {
            PythonSampleConsole test = new PythonSampleConsole();
            test.Start();

            doCommand(test);

            test.Stop();
        }
        
        static void printHelp()
        {
            Console.Clear();
            Console.Write("========== Welcome PythonTest ==========\n");
            Console.Write("quit          quit this Console\n");
            Console.Write("stop          stop PythonTest\n");
            Console.Write("push [msg]    send a test msg\n");
            Console.Write("help          this manual\n");
        }
        static void doCommand(PythonSampleConsole test)
        {
            bool running = true;
            printHelp();
            while (running)
            {
                string[] temp = Console.ReadLine().Split();
                if (temp == null || temp.Length < 0 || temp[0].Trim().Length <= 0)
                    continue;
                string command = temp[0].Trim();

                if ("quit".Equals(command.ToLower()))
                    running = false;
                else if ("stop".Equals(command.ToLower()))
                    test.Stop();
                else if ("help".Equals(command.ToLower()))
                    printHelp();
                else if("push".Equals(command.ToLower()) && temp.Length > 0)
                {
                    test.PushMsg(temp[1].Trim());
                }
            };
        }

        public PythonSampleConsole()
        {
            _engine = new PythonEngineImpl();
            _engine.OnPythonEvent = OnResut;
        }
        public void Start()
        {
            _sourceCode = String.Join("\r", _sourceCodeLines); // aggregate source code(multiple lines)
            if (_engine != null)
                _engine.Start(_sourceCode);
        }

        public void Stop()
        {
            if (_engine != null)
                _engine.Stop();
        }

        public void PushMsg(string msg)
        {
            if (_engine != null)
                _engine.PushMsg(msg);
        }
        public void OnResut(object content, string msg)
        {
            if(content == null)
                Console.Out.WriteLine(string.Format(">> msg[{0}]", msg));
            else
                Console.Out.WriteLine(string.Format(">> content[{0}] msg[{0}]", content.ToString(), msg));
        }
    }
}
