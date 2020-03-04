using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nito.AsyncEx.Synchronous;
using SoundFingerprinting;
using SoundFingerprinting.Audio;
using SoundFingerprinting.Builder;
using SoundFingerprinting.DAO.Data;
using SoundFingerprinting.Data;
using SoundFingerprinting.Emy;
using SoundFingerprinting.InMemory;
using NAudio.Wave;
using System.IO;

namespace SoundtrackSeekerCE
{
    class Program
    {
        private static readonly IModelService modelService = new InMemoryModelService(); // store fingerprints in RAM.
        private static readonly IAudioService audioService = new SoundFingerprintingAudioService(); // default audio library. 
        private static readonly EmyModelService emyModelService = EmyModelService.NewInstance("localhost", 3399); // connect to Emy on port 3399. Is it necessary to connect in each method? I'll investigate later.       

        private static WaveInEvent waveSource = null;
        private static WaveFileWriter waveFile = null;
        private const int SECONDS_TO_LISTEN = 13;
        public static string tempFile = "";
        private static string userInput = "";

        static void Main(string[] args)
        {
            #region Tracks hashed at home.
            //Dictionary<string, string> metaFieldForTrack1 = new Dictionary<string, string>();
            //metaFieldForTrack1.Add("Album", "Metal Gear (MSX)");
            //Dictionary<string, string> metaFieldForTrack2 = new Dictionary<string, string>();
            //metaFieldForTrack2.Add("Album", "Street Fighter III: New Generation");
            //Dictionary<string, string> metaFieldForTrack3 = new Dictionary<string, string>();
            //metaFieldForTrack3.Add("Album", "Ape Escape");

            //string trackPath1 = "Test Audio for Storage/01_theme_of_tara.wav";
            //string trackPath2 = "Test Audio for Storage/02_leave_alone.wav";
            //string trackPath3 = "Test Audio for Storage/03_galaxy_monkey.wav";

            //var track1Info = new TrackInfo(Guid.NewGuid().ToString(), "Theme of Tara", "KONAMI KuKeiHa CLUB");
            //var track2Info = new TrackInfo(Guid.NewGuid().ToString(), "Leave Alone", "Yuki Iwai");
            //var track3Info = new TrackInfo(Guid.NewGuid().ToString(), "Galaxy Monkey", "Soichi Terada");
            #endregion

            #region New tracks to hash.
            //Dictionary<string, string> metaFieldForTrack4 = new Dictionary<string, string>();
            //metaFieldForTrack4.Add("Album", "Street Fighter III: New Generation");
            //Dictionary<string, string> metaFieldForTrack5 = new Dictionary<string, string>();
            //metaFieldForTrack5.Add("Album", "Street Fighter III: 2nd Impact");
            //Dictionary<string, string> metaFieldForTrack6 = new Dictionary<string, string>();
            //metaFieldForTrack6.Add("Album", "D4: Dark Dreams Don't Die");

            //string trackPath4 = "Test Audio for Storage/04_good_fighter.wav";
            //string trackPath5 = "Test Audio for Storage/05_good_fighter_2nd_edit.wav";
            //string trackPath6 = "Test Audio for Storage/06_urban_closet.wav";

            //var track4Info = new TrackInfo(Guid.NewGuid().ToString(), "Good Fighter", "Hideki Okugawa");
            //var track5Info = new TrackInfo(Guid.NewGuid().ToString(), "Good Fighter ~2nd Edit", "Hideki Okugawa");
            //var track6Info = new TrackInfo(Guid.NewGuid().ToString(), "Urban Closet", "MANYO");
            #endregion

            #region Hashing (Do not hash pre-hashed tracks).
            // At home.
            //HashTrack(trackPath1, metaFieldForTrack1, track1Info);
            //HashTrack(trackPath2, metaFieldForTrack2, track2Info);
            //HashTrack(trackPath3, metaFieldForTrack3, track3Info);

            // New tracks to hash (Now hashed at home).
            //HashTrack(trackPath4, metaFieldForTrack4, track4Info);
            //HashTrack(trackPath5, metaFieldForTrack5, track5Info);
            //HashTrack(trackPath6, metaFieldForTrack6, track6Info);
            #endregion

            do
            {
                Console.WriteLine("Welcome to Soundtrack Seeker Console Edition!");
                Console.WriteLine("Enter any input to seek.");
                Console.WriteLine("\nType 'quit' if you want to exit Soundtrack Seeker.\n");
                //Console.WriteLine("\n0: Insert Track 1\n1: Insert Track 3\n2: Query Track 1\n3: Query 'Leave Alone'\n4: Seek...");
                userInput = Console.ReadLine().Trim();
                if(userInput.ToUpper() != "QUIT") Seek();

                #region Old switch.
                //switch (userInput)
                //{               
                //    case "0": // INSERT TRACK 1.                      
                //        validInput = true;
                //        Console.WriteLine("Case 0 active.");
                //        var task0 = Task.Run(async () => await StoreForLaterRetrievalAsync(trackPath3, metaFieldForTrack3, track3Info));
                //        task0.WaitAndUnwrapException();

                //        // https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.run?view=netframework-4.8
                //        // https://stackoverflow.com/questions/9343594/how-to-call-asynchronous-method-from-synchronous-method-in-c
                //        // https://stackoverflow.com/questions/13046174/how-should-i-use-static-method-classes-within-async-await-operations
                //        break;

                //    case "1": // INSERT TRACK 3.
                //        validInput = true;
                //        Console.WriteLine("Case 1 active.");
                //        var task1 = Task.Run(async () => await StoreForLaterRetrievalAsync(trackPath3, metaFieldForTrack3, track3Info));
                //        task1.WaitAndUnwrapException();
                //        break;

                //    case "2": // Query Track 1.
                //        validInput = true;
                //        //EmyRegisterBestMatchesForSong(trackPath1);
                //        var task2 = Task.Run(async () => await GetBestMatchForSong(trackPath1));
                //        TrackData td2 = task2.WaitAndUnwrapException<TrackData>();
                //        bool albumFound2 = td2.MetaFields.TryGetValue("Album", out string value2); // I'd like to get the metafields more gracefully if I can.
                //        Console.WriteLine("\nMatch!\nTitle: {0}\nAlbum: {1}", td2.Title, value2);
                //        break;

                //    case "3": // Query Track 2.
                //        validInput = true;
                //        //EmyRegisterBestMatchesForSong(trackPath2); 
                //        var task3 = Task.Run(async () => await GetBestMatchForSong(trackPath2));
                //        TrackData td3 = task3.WaitAndUnwrapException<TrackData>();
                //        if (td3 != null)
                //        {
                //            bool albumFound3 = td3.MetaFields.TryGetValue("Album", out string value3);
                //            Console.WriteLine("\nMatch!\nTitle: {0}\nAlbum: {1}", td3.Title, value3);
                //        }
                //        break;                

                //    case "4": // Query Track 2.
                //        validInput = true;                                                                     
                //        break;

                //    default:
                //        validInput = false;
                //        break;
                //}               
                #endregion
            }
            while (userInput.ToUpper() != "QUIT");

            if (userInput.ToUpper() == "QUIT")
            {
                Console.WriteLine("\nThank you for using Soundtrack Seeker Console Edition.\nHave a nice day!\n");
                Environment.Exit(0);
            }
        }

        static void waveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            waveFile.Write(e.Buffer, 0, e.BytesRecorded);
        }

        // STORAGE
        public static void HashTrack(string trackPathIn, Dictionary<string, string> metaFieldsIn, TrackInfo trackInfoIn)
        {
            var task0 = Task.Run(async () => await StoreForLaterRetrievalAsync(trackPathIn, metaFieldsIn, trackInfoIn));
            task0.WaitAndUnwrapException();
        }

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
        public static async Task<TrackData> GetBestMatchForSong(string queryAudioFile)
        {
            //Console.WriteLine("'GetBestMatchForSong' method activated.");
            // Connect to Emy on port 3399.
            //var emyModelService = EmyModelService.NewInstance("localhost", 3399);

            int secondsToAnalyze = 10; // number of seconds to analyze from query file.
            int startAtSecond = 0; // start at the beginning.

            // query the underlying database for similar audio sub-fingerprints.
            var queryResult = await QueryCommandBuilder.Instance.BuildQueryCommand()
                                                 .From(queryAudioFile, secondsToAnalyze, startAtSecond)
                                                 .UsingServices(emyModelService, audioService)
                                                 .Query();

            //// Register matches so that they appear in the dashboard.					
            //if(queryResult != null) emyModelService.RegisterMatches(queryResult.ResultEntries); //This line still causes issues.

            try
            {
                return queryResult.BestMatch.Track;
            }
            catch (NullReferenceException nre)
            {
                Console.WriteLine("\nTrack not found! Please try moving your microphone closer to the audio source.\n");
                return null;
            }
        }
        public static async void EmyRegisterBestMatchesForSong(string queryAudioFile) // Not in use currently.
        {
            Console.WriteLine("'EmyRegisterBestMatchesForSong' method activated.");
            // Connect to Emy on port 3399.
            //var emyModelService = EmyModelService.NewInstance("localhost", 3399);

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

        private static void Seek()
        { // https://stackoverflow.com/questions/24607351/recording-microphone-audio-using-naudio-in-a-console-application Used for reference.
            Console.WriteLine("\nNow recording...");

            waveSource = new WaveInEvent();

            waveSource.WaveFormat = new NAudio.Wave.WaveFormat(44100, 1);
            waveSource.DataAvailable += new EventHandler<WaveInEventArgs>(waveSource_DataAvailable);

            tempFile = @"Test Audio for Storage\query.wav";
            waveFile = new WaveFileWriter(tempFile, waveSource.WaveFormat);
            waveSource.StartRecording();

            var task = Task.Run(async () => await CeaseListening(waveSource, waveFile, SECONDS_TO_LISTEN));
            task.WaitAndUnwrapException();
        }

        public static async Task CeaseListening(WaveInEvent waveSource, WaveFileWriter waveFile, int seconds)
        {
            await Task.Delay(seconds * 1000);
            waveSource.StopRecording();
            waveFile.Dispose();

            //Console.WriteLine("\nFinished listening. Ready to seek!");

            var task = Task.Run(async () => await GetBestMatchForSong(tempFile));
            TrackData td = task.WaitAndUnwrapException<TrackData>();
            if (td != null)
            {
                bool albumFound = td.MetaFields.TryGetValue("Album", out string value3);                
                Console.WriteLine("\nMatch!\nTitle: {0}\nAlbum: {1}\n", td.Title, value3);
            }
        }

        // DELETION - To figure out later.
        //private static void DeleteTrackFingerprint(string trackID)
        //{
        //    modelService.DeleteTrack(trackID);
        //}         
    }
}