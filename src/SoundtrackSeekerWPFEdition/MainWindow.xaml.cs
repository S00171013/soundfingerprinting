using NAudio.Wave;
using SoundFingerprinting;
using SoundFingerprinting.Audio;
using SoundFingerprinting.Builder;
using SoundFingerprinting.DAO;
using SoundFingerprinting.DAO.Data;
using SoundFingerprinting.Emy;
using SoundFingerprinting.InMemory;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SoundtrackSeekerWPFEdition
{
    public partial class MainWindow : Window
    {
        private IAdvancedModelService modelService; // store fingerprints in RAM.
        private static readonly IAudioService audioService = new SoundFingerprintingAudioService(); // default audio library. 
        private EmyModelService emyModelService = EmyModelService.NewInstance("localhost", 3399); // connect to Emy on port 3399.       

        private static WaveInEvent waveSource = null;
        private static WaveFileWriter waveFile = null;
        private const int SECONDS_TO_LISTEN = 13;
        public static string tempFile = "";
        private static string lastMatchedSongId;

        private ITrackDao trackDao;
        //private IModelReference imr;

        public MainWindow()
        {
            InitializeComponent();

            //var ramStorage = new RAMStorage(25);
            //trackDao = new TrackDao(ramStorage);

            modelService = new InMemoryModelService();
        }

        private void btnSeek_Click(object sender, RoutedEventArgs e)
        {
            HandleVisibility(lblListenMessage, "DISPLAY");
            HandleVisibility(btnSeek, "HIDE");
            SetTrackInfoVisibility("HIDE");

            waveSource = new WaveInEvent();

            waveSource.WaveFormat = new NAudio.Wave.WaveFormat(44100, 1);
            waveSource.DataAvailable += new EventHandler<WaveInEventArgs>(waveSource_DataAvailable);

            tempFile = @"Queries\query.wav";
            waveFile = new WaveFileWriter(tempFile, waveSource.WaveFormat);
            waveSource.StartRecording();

            var task = Task.Run(async () => await CeaseListening(waveSource, waveFile, SECONDS_TO_LISTEN));
            //task.WaitAndUnwrapException();
        }

        static void waveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            waveFile.Write(e.Buffer, 0, e.BytesRecorded);
        }

        public async Task CeaseListening(WaveInEvent waveSource, WaveFileWriter waveFile, int seconds)
        {
            await Task.Delay(seconds * 1000);
            waveSource.StopRecording();
            waveFile.Dispose();

            Task<TrackData> task = Task.Run(async () => await GetBestMatchForSong(tempFile));

            task.Wait();

            TrackData td = task.Result; // WaitAndUnwrap here initially.            
            if (td != null)
            {
                bool albumFound = td.MetaFields.TryGetValue("Album", out string foundAlbum);

                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    lblTitle.Content = td.Title;
                    lblAlbum.Content = foundAlbum;
                    lblArtist.Content = td.Artist;
                    //trackDao.DeleteTrack(td.TrackReference);
                    //lastMatchedSongId = td.Id;
                    //DeletionTest(lastMatchedSongId); // Don't leave this uncommented!


                    SetTrackInfoVisibility("DISPLAY"); // Gotta check this out at home. Have a feeling changes won't be made while the labels are invisible.
                }), DispatcherPriority.Render);
            }

            await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                HandleVisibility(lblListenMessage, "HIDE");
                HandleVisibility(btnSeek, "DISPLAY");

            }), DispatcherPriority.Render);
        }

        public async Task<TrackData> GetBestMatchForSong(string queryAudioFile)
        {
            int secondsToAnalyze = 10; // number of seconds to analyze from query file.
            int startAtSecond = 0; // start at the beginning.

            // query the underlying database for similar audio sub-fingerprints.
            var queryResult = await QueryCommandBuilder.Instance.BuildQueryCommand()
                                                 .From(queryAudioFile, secondsToAnalyze, startAtSecond)
                                                 .UsingServices(emyModelService, audioService)
                                                 .Query();

            //// Register matches so that they appear in the dashboard.					
            //if(queryResult != null) emyModelService.RegisterMatches(queryResult.ResultEntries); //This line still causes issues. I should try this again with the new version.

            try
            {
                return queryResult.BestMatch.Track;
            }
            catch (NullReferenceException nre)
            {
                MessageBox.Show("Track not found! Please try moving your microphone closer to the audio source.");
                return null;
            }
        }

        public void HandleVisibility(ContentControl uiElement, string action)
        {
            if (action.ToUpper() == "DISPLAY") uiElement.Visibility = Visibility.Visible;
            else if (action.ToUpper() == "HIDE") uiElement.Visibility = Visibility.Hidden;
            else if (action.ToUpper() == "COLLAPSE") uiElement.Visibility = Visibility.Collapsed;
        }

        public void SetTrackInfoVisibility(string action)
        {
            HandleVisibility(lblTitle, action);
            HandleVisibility(lblAlbum, action);
            HandleVisibility(lblArtist, action);
        }

        // ADMIN Methods
        private void DeletionTest(string trackId)
        {
            //TrackData trackToDelete = trackDao.ReadTrackById(trackID);

            //MessageBox.Show("For deletion: {0}", trackToDelete.Title);

            //imr.Id = trackID;
            //try
            //{
            emyModelService.DeleteTrack(trackId);
            
            //}
            //catch(Exception e)
            //{
            //    MessageBox.Show(e.Message);
            //}

            //modelService.DeleteTrack(trackID);           
        }

        private void btnDeleteTest_Click(object sender, RoutedEventArgs e)
        {
            //DeletionTest("GPPDS1989360"); // Leave Alone deletion test.  
            DeletionTest("c50f8b3b-4e65-474f-b552-63a936aa7d62"); // Deletion Test. It worked in D2037. Gotta test this again at home. 
            // Could it be that it was a simple sound effect being deleted? Could it be the GUID? Further investigation needed.
        }
    }
}