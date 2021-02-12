using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Renci.SshNet;

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
            for (int i = 2; i < args.Length; i++)
            {
                names.Add(new CharacterFind{CharacterName = args[i]});
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
                    using (LocalDbContext dbContext = new LocalDbContext())
                    {
                        foreach(CharacterFind characterFind in names.Where(x => !x.Found))
                        {
                            CharacterInfo user = dbContext.Users
                            .Where(first =>
                                first.name == characterFind.CharacterName)
                            .FirstOrDefault();

                            using (var client = new SshClient("192.168.41.28", 22, "root", "F1reF0x"))
                            {
                                client.Connect();
                                string command = $"/usr/local/bin/docker-compose -f /opt/Seat/docker-compose.yml --project-directory /opt/Seat/ exec -T seat-web su -c 'php artisan esi:job:dispatch \"Seat\\\\Eveapi\\\\Jobs\\\\Location\\\\Character\\\\Online\" --character_id={user.character_id}'";
                                //string command = "echo test";
                                using(var cmd = client.CreateCommand(command))
                                {
                                    cmd.Execute();
                                    if (cmd.ExitStatus != 0)
                                    {
                                        Console.WriteLine("Command>" + cmd.CommandText);
                                        Console.WriteLine("Result>" + cmd.Result);
                                        Console.WriteLine("Error>" + cmd.Error);
                                        Console.WriteLine("Return Value = {0}", cmd.ExitStatus);
                                    }
                                }
                                client.Disconnect();
                            }
                        }
                    }

                    Thread.Sleep(20000);

                    using (LocalDbContext dbContext = new LocalDbContext())
                    {
                        foreach(CharacterFind characterFind in names.Where(x => !x.Found))
                        {
                            CharacterInfo user = dbContext.Users
                            .Where(first =>
                                first.name == characterFind.CharacterName)
                            .FirstOrDefault();

                            CharacterOnline online = dbContext.Online
                            .Where(first =>
                                first.character_id == user.character_id)
                            .FirstOrDefault();

                            if (online.online || online.last_login > startedTime)
                            {
                                CharacterFind result = names.FirstOrDefault(x => characterFind.CharacterName == x.CharacterName);
                                if (result != null)
                                {
                                    result.Found = true;
                                }
                            }
                        }
                    }

                    Thread.Sleep(60000);
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
    }
}
