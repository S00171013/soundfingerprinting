﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Nito.AsyncEx.Synchronous;
using SoundFingerprinting;
using SoundFingerprinting.Audio;
using SoundFingerprinting.Builder;
using SoundFingerprinting.DAO.Data;
using SoundFingerprinting.Data;
using SoundFingerprinting.Emy;
using SoundFingerprinting.InMemory;

namespace SoundtrackSeekerCE
{
    class Program
    {
        //private readonly IModelService modelService = new InMemoryModelService(); // store fingerprints in RAM.
        private static readonly IAudioService audioService = new SoundFingerprintingAudioService(); // default audio library. 
        //private readonly EmyModelService emyModelService = EmyModelService.NewInstance("localhost", 3399); // connect to Emy on port 3399. Is it necessary to connect in each method? I'll investigate later.       

        static void Main(string[] args)
        {
            Dictionary<string, string> metaFieldForTrack1 = new Dictionary<string, string>();
            metaFieldForTrack1.Add("Album", "Metal Gear (MSX)");
            Dictionary<string, string> metaFieldForTrack2 = new Dictionary<string, string>();
            metaFieldForTrack2.Add("Album", "Street Fighter III: New Generation");
            Dictionary<string, string> metaFieldForTrack3 = new Dictionary<string, string>();
            metaFieldForTrack3.Add("Album", "Ape Escape");

            string trackPath1 = "Test Audio for Storage/01_theme_of_tara.wav";
            string trackPath2 = "Test Audio for Storage/02_leave_alone.wav";
            string trackPath3 = "Test Audio for Storage/03_galaxy_monkey.wav";

            var track3Info = new TrackInfo("GBDFS2385164", "Galaxy Monkey", "Soichi Terada", 201, metaFieldForTrack3); // Define track 3 info.

            bool validInput;

            do
            {
                Console.WriteLine("A simple Emy Test.");
                Console.WriteLine("\nSelect an option.");
                Console.WriteLine("\n0: Insert Track 1\n1: Insert Track 3\n2: Query Track 1\n3: Query Track 2\n");
                string userInput = Console.ReadLine().Trim();

                switch (userInput)
                {
                    case "0":
                        validInput = true;
                        Console.WriteLine("Case 0 active.");
                        var task0 = Task.Run(async () => await StoreForLaterRetrievalAsync(trackPath3, metaFieldForTrack3, track3Info));
                        task0.WaitAndUnwrapException();

                        // https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.run?view=netframework-4.8
                        // https://stackoverflow.com/questions/9343594/how-to-call-asynchronous-method-from-synchronous-method-in-c
                        // https://stackoverflow.com/questions/13046174/how-should-i-use-static-method-classes-within-async-await-operations
                        break;

                    case "1":
                        validInput = true;
                        Console.WriteLine("Case 1 active.");
                        var task1 = Task.Run(async () => await StoreForLaterRetrievalAsync(trackPath3, metaFieldForTrack3, track3Info));
                        task1.WaitAndUnwrapException();
                        break;

                    case "2":
                        validInput = true;
                        //EmyRegisterBestMatchesForSong(trackPath1);
                        var task2 = Task.Run(async () => await GetBestMatchForSong(trackPath1));
                        TrackData td2 = task2.WaitAndUnwrapException<TrackData>();
                        bool albumFound2 = td2.MetaFields.TryGetValue("Album", out string value2); // I'd like to get the metafields more gracefully if I can.
                        Console.WriteLine("\nMatch!\nTitle: {0}\nAlbum: {1}", td2.Title, value2);
                        break;

                    case "3":
                        validInput = true;
                        //EmyRegisterBestMatchesForSong(trackPath2); 
                        var task3 = Task.Run(async () => await GetBestMatchForSong(trackPath2));
                        TrackData td3 = task3.WaitAndUnwrapException<TrackData>();
                        bool albumFound3 = td3.MetaFields.TryGetValue("Album", out string value3);
                        Console.WriteLine("\nMatch!\nTitle: {0}\nAlbum: {1}", td3.Title, value3);
                        break;

                    default:
                        validInput = false;
                        break;
                }
            }
            while (validInput == false);
        }

        // STORAGE
        public static async Task StoreForLaterRetrievalAsync(string pathToAudioFile, Dictionary<string, string> metaFieldIn, TrackInfo trackInfoIn)
        {           
            // Connect to Emy on port 3399.
            var emyModelService = EmyModelService.NewInstance("localhost", 3399);

            //// TrackInfo from metadata.
            //var track = new TrackInfo("GBBKS1200164", "Theme of Tara", "KONAMI KuKeiHa CLUB", 201, metaFieldIn); // Define track info.

            // Create fingerprints.
            var hashedFingerprints = await FingerprintCommandBuilder.Instance
                                        .BuildFingerprintCommand()
                                        .From(pathToAudioFile)
                                        .UsingServices(audioService)
                                        .Hash();

            // Store hashes in the database for later retrieval.
            emyModelService.Insert(trackInfoIn, hashedFingerprints);
        }

        // QUERIES        
        public static async void EmyRegisterBestMatchesForSong(string queryAudioFile)
        {
            Console.WriteLine("'EmyRegisterBestMatchesForSong' method activated.");
            // Connect to Emy on port 3399.
            var emyModelService = EmyModelService.NewInstance("localhost", 3399);

            int secondsToAnalyze = 10; // number of seconds to analyze from query file.
            int startAtSecond = 0; // start at the beginning.

            // Query Emy database.
            var queryResult = await QueryCommandBuilder.Instance.BuildQueryCommand()
                                                    .From(queryAudioFile, secondsToAnalyze, startAtSecond)
                                                    .UsingServices(emyModelService, audioService)
                                                    .Query();

            // Register matches so that they appear in the dashboard.					
            emyModelService.RegisterMatches(queryResult.ResultEntries);
        }
        public static async Task<TrackData> GetBestMatchForSong(string queryAudioFile)
        {
            Console.WriteLine("'GetBestMatchForSong' method activated.");
            // Connect to Emy on port 3399.
            var emyModelService = EmyModelService.NewInstance("localhost", 3399);

            int secondsToAnalyze = 10; // number of seconds to analyze from query file.
            int startAtSecond = 0; // start at the beginning.

            // query the underlying database for similar audio sub-fingerprints.
            var queryResult = await QueryCommandBuilder.Instance.BuildQueryCommand()
                                                 .From(queryAudioFile, secondsToAnalyze, startAtSecond)
                                                 .UsingServices(emyModelService, audioService)
                                                 .Query();

            //// Register matches so that they appear in the dashboard.					
            //if(queryResult != null) emyModelService.RegisterMatches(queryResult.ResultEntries); //This line still causes issues.

            return queryResult.BestMatch.Track;
        }
    }
}