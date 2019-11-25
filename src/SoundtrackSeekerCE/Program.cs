using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nito.AsyncEx;
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
        private readonly IAudioService audioService = new SoundFingerprintingAudioService(); // default audio library. 
        //private readonly EmyModelService emyModelService = EmyModelService.NewInstance("localhost", 3399); // connect to Emy on port 3399. Is it necessary to connect in each method? I'll investigate later.

        static void Main(string[] args)
        {
            Dictionary<string, string> metaFieldForTrack1 = new Dictionary<string, string>();
            metaFieldForTrack1.Add("Album", "Metal Gear (MSX)");
            Dictionary<string, string> metaFieldForTrack2 = new Dictionary<string, string>();
            metaFieldForTrack2.Add("Album", "Street Fighter III: New Generation");

            string trackPath1 = "Test Audio for Storage/01_theme_of_tara.mp3";
            string trackPath2 = "Test Audio for Storage/02_leave_alone.mp3";

            bool validInput;

            do
            {
                Console.WriteLine("A simple Emy Test.");
                Console.WriteLine("\nSelect an option.");
                Console.WriteLine("\n0: Insert Track 1\n1: Insert Track 2\n2: Query Track 1\n3: Query Track 2\n");
                string userInput = Console.ReadLine().Trim();

                switch (userInput)
                {
                    case "0":
                        validInput = true;
                        //var task = Task.Run(async () => await StoreForLaterRetrievalAsync(trackPath1, metaFieldForTrack1));
                        //var result = task.WaitAndUnwrapException();
                        //var t = Task.Run(StoreForLaterRetrievalAsync(trackPath1, metaFieldForTrack1));

                        // https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.run?view=netframework-4.8
                        // https://stackoverflow.com/questions/9343594/how-to-call-asynchronous-method-from-synchronous-method-in-c
                        break;

                    case "1":
                        validInput = true;
                        break;

                    case "2":
                        validInput = true;
                        break;

                    case "3":
                        validInput = true;
                        break;

                    default:
                        validInput = false;
                        break;
                }
            }
            while (validInput == false);
        }

        // STORAGE
        public async Task StoreForLaterRetrievalAsync(string pathToAudioFile, Dictionary<string, string> metaFieldIn)
        {
            // Connect to Emy on port 3399.
            var emyModelService = EmyModelService.NewInstance("localhost", 3399);

            // TrackInfo from metadata.
            var track = new TrackInfo("GBBKS1200164", "Theme of Tara", "KONAMI KuKeiHa CLUB", 201, metaFieldIn); // Define track info.

            // Create fingerprints.
            var hashedFingerprints = await FingerprintCommandBuilder.Instance
                                        .BuildFingerprintCommand()
                                        .From(pathToAudioFile)
                                        .UsingServices(audioService)
                                        .Hash();

            // Store hashes in the database for later retrieval.
            emyModelService.Insert(track, hashedFingerprints);
        }

        // QUERIES        
        public async void EmyRegisterBestMatchesForSong(string queryAudioFile)
        {
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

        public async Task<TrackData> GetBestMatchForSong(string queryAudioFile)
        {
            // Connect to Emy on port 3399.
            var emyModelService = EmyModelService.NewInstance("localhost", 3399);

            int secondsToAnalyze = 10; // number of seconds to analyze from query file.
            int startAtSecond = 0; // start at the beginning.

            // query the underlying database for similar audio sub-fingerprints.
            var queryResult = await QueryCommandBuilder.Instance.BuildQueryCommand()
                                                 .From(queryAudioFile, secondsToAnalyze, startAtSecond)
                                                 .UsingServices(emyModelService, audioService)
                                                 .Query();

            return queryResult.BestMatch.Track;
        }
    }
}