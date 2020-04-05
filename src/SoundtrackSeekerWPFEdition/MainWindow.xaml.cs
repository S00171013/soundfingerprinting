using HundredMilesSoftware.UltraID3Lib;
using NAudio.Wave;
using SoundFingerprinting;
using SoundFingerprinting.Audio;
using SoundFingerprinting.Audio.NAudio;
using SoundFingerprinting.Builder;
using SoundFingerprinting.DAO;
using SoundFingerprinting.DAO.Data;
using SoundFingerprinting.Data;
using SoundFingerprinting.Emy;
using SoundFingerprinting.InMemory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SoundtrackSeekerWPFEdition
{
    public partial class MainWindow : Window
    {
        private static readonly IAudioService audioService = new SoundFingerprintingAudioService(); // default audio library. 
        private static EmyModelService emyModelService = EmyModelService.NewInstance("localhost", 3399); // connect to Emy on port 3399.       
        private static readonly IAudioService nAudioService = new NAudioService();

        UltraID3 u = new UltraID3();
        private static WaveInEvent waveSource = null;
        private static WaveFileWriter waveFile = null;
        private const int SECONDS_TO_LISTEN = 13;
        public static string tempFile = "";
        private static string lastMatchedSongId;

        public MainWindow()
        {
            //EmyModelService emyModelService = EmyModelService.NewInstance("localhost", 3399);
            InitializeComponent();
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
        #region TRACK HASHING
        private void HashTrack(string trackPathIn, TrackInfo trackInfoIn) // We want to pass in a list of "TrackInfo" objects here.
        {
            lbxOutput.Items.Add(String.Format("Hashing Track: '{0}' by {1}.", trackInfoIn.Title, trackInfoIn.Artist));            
            var task = Task.Run(async () => await StoreForLaterRetrievalAsync(trackPathIn, trackInfoIn));
            task.Wait(); // WaitAndUnwrapException in the console version. May have to fix something here.            
        }

        private TrackInfo ExtractTrackInfo(string trackFilePathIn)
        {
            u.Read(trackFilePathIn);            

            // Metafield setup.
            Dictionary<string, string> newTrackMetaField = new Dictionary<string, string>();
            newTrackMetaField.Add("Album", u.Album);
            newTrackMetaField.Add("Genre", u.Genre);
            newTrackMetaField.Add("Year", u.Year.ToString());
            newTrackMetaField.Add("Duration", Math.Round(u.Duration.TotalSeconds, 2).ToString());

            TrackInfo extractedTrackInfo = new TrackInfo(Guid.NewGuid().ToString(), u.Title, u.Artist, newTrackMetaField, MediaType.Audio);
            return extractedTrackInfo;
        }

        private void btnHashTracks_Click(object sender, RoutedEventArgs e)
        {
            //string sourceDirectory = @"C:\Users\Jack\Downloads\Test";
            btnHashTracks.Visibility = Visibility.Hidden;
            lblHashingMessage.Visibility = Visibility.Visible;
            List<string> filesToHash = new List<string>();

            //MessageBox.Show("Commencing hashing.");

            // Get track info.  
            try
            {
                filesToHash = Directory.EnumerateFiles(tbxAdminInput.Text, "*.mp3").ToList();               

                foreach(string trackFilePath in filesToHash)
                {
                    TrackInfo newTrackInfo = ExtractTrackInfo(trackFilePath);
                    HashTrack(trackFilePath, newTrackInfo);
                }

                lblHashingMessage.Visibility = Visibility.Hidden;
                btnHashTracks.Visibility = Visibility.Visible;
            }
            catch(Exception ex)
            {
                //MessageBox.Show("Invalid directory input.");
                MessageBox.Show(ex.Message);
                lblHashingMessage.Visibility = Visibility.Hidden;
                btnHashTracks.Visibility = Visibility.Visible;               
            }
        }

        public static async Task StoreForLaterRetrievalAsync(string pathToAudioFile, TrackInfo trackInfoIn)
        {
            // Connect to Emy on port 3399.
            //var emyModelService = EmyModelService.NewInstance("localhost", 3399); // It seems that it's unnecessary to create a new connection each time.   

            // Create fingerprints.
            var hashedFingerprints = await FingerprintCommandBuilder.Instance
                                        .BuildFingerprintCommand()
                                        .From(pathToAudioFile)
                                        .UsingServices(nAudioService)
                                        .Hash();

            // Store hashes in the database for later retrieval.
            emyModelService.Insert(trackInfoIn, hashedFingerprints);
        }
        #endregion

        #region TRACK HASH DELETION
        private void DeleteTrack(string trackId)
        {
            TrackInfo trackToDelete = null;

            trackToDelete = emyModelService.ReadTrackById(trackId);

            if (trackToDelete != null)
            {
                emyModelService.DeleteTrack(trackId);
                bool albumFound = trackToDelete.MetaFields.TryGetValue("Album", out string album);
                MessageBox.Show(String.Format("{0}'s track '{1}' from the album '{2}' has been deleted.",
                    trackToDelete.Artist, trackToDelete.Title, album));

                trackToDelete = null;
                lblDeletingMessage.Visibility = Visibility.Hidden;
                btnDeleteTrack.Visibility = Visibility.Visible;               
            }

            else if (trackToDelete == null)
            {
                MessageBox.Show(String.Format("No track exists with this ID: {0}", trackId));
                lblDeletingMessage.Visibility = Visibility.Hidden;
                btnDeleteTrack.Visibility = Visibility.Visible;
            }
        }
        private void btnDeleteTrack_Click(object sender, RoutedEventArgs e)
        {
            btnDeleteTrack.Visibility = Visibility.Hidden;
            lblDeletingMessage.Visibility = Visibility.Visible;
            DeleteTrack(tbxAdminInput.Text);                                             
        }
        #endregion
    }
}