using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

namespace CharacterLauncher
{
    class Program
    {
        static void Main(string[] args)
        {
            ActualWork actualWork = new ActualWork();
            actualWork.main(args);
        }
    }

    public class ActualWork
    {
        ConcurrentBag<CharacterFind> names = new ConcurrentBag<CharacterFind>();
        private static BackgroundWorker worker = new BackgroundWorker();

        public void main(string[] args)
        {
            worker.DoWork += worker_DoWork;
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;

            worker.RunWorkerAsync();

            bool run = true;

            if (args[2].Contains("\\"))
            {
                string filePath = args[2];
                if (File.Exists(filePath))
                {
                    string jsonContent = File.ReadAllText(filePath);
                    var characters = JsonSerializer.Deserialize<List<CharacterFind>>(jsonContent);
                    foreach (var character in characters)
                    {
                        names.Add(character);
                    }
                }
                else
                {
                    Console.WriteLine("File not found: " + filePath);
                }
            }
            else
            {
                for (int i = 2; i < args.Length; i++)
                {
                    names.Add(new CharacterFind{CharacterName = args[i]});
                }
            }

            while(run)
            {
                CycleThings(int.Parse(args[1]), args[0]);

                if (names.All(x => x.Found))
                {
                    run = false;
                }

                Thread.Sleep(60000);
            }
        }

        private void CycleThings(int maxOpen, string processString)
        {
            foreach(CharacterFind characterFind in names)
            {
                if (!characterFind.Found && characterFind.LastLaunched < DateTime.Now.AddMinutes(-5))
                {
                    bool accountOpen = false;
                    foreach (Process proc in Process.GetProcessesByName("ExeFile"))
                    {
                        string characterName = proc.MainWindowTitle.Replace("EVE - ", "");

                        CharacterFind result = names.FirstOrDefault(x => characterName == x.CharacterName);
                        if (result != null && result.EVEAccountName == characterFind.EVEAccountName)
                        {
                            accountOpen = true;
                        }
                    }

                    if (accountOpen)
                    {
                        continue;
                    }

                    Console.WriteLine($"{characterFind.CharacterName} - Completed: {names.Count(x => x.Found)}/{names.Count()}");

                    Process.Start(processString, $"-dx11 -tranquility -eve \"{characterFind.CharacterName}\"");

                    characterFind.LastLaunched = DateTime.Now;
                    
                    while (Process.GetProcessesByName("ExeFile").Length >= maxOpen || names.All(x => x.Found))
                    {
                        Thread.Sleep(5000);
                        
                        if (names.All(x => x.Found))
                        {
                            return;
                        }
                    }

                    Thread.Sleep(5000);
                }
            }
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            DateTime startedTime = DateTime.UtcNow;

            BackgroundWorker(startedTime);
        }

        private void BackgroundWorker(DateTime startedTime)
        {
            try
            {
                while(true)
                {
                    foreach (Process proc in Process.GetProcessesByName("ExeFile"))
                    {
                        string characterName = proc.MainWindowTitle.Replace("EVE - ", "");

                        CharacterFind result = names.FirstOrDefault(x => characterName == x.CharacterName);
                        if (result != null)
                        {
                            result.Found = true;
                        }
                    }

                    Thread.Sleep(1000);
                }
            }
            catch (Exception error)
            {
                Console.WriteLine($"Crashy : {error.Message}");

                BackgroundWorker(startedTime);
            }
        }
    }

    public class CharacterFind
    {
        public string CharacterName { get; set; }

        public bool Found { get; set; }
        
        public DateTime LastLaunched { get; set; }
        public string EVEAccountName { get; set; }
    }
}
