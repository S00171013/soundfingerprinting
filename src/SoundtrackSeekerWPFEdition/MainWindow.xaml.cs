﻿using NAudio.Wave;
using SoundFingerprinting;
using SoundFingerprinting.Audio;
using SoundFingerprinting.Builder;
using SoundFingerprinting.DAO;
using SoundFingerprinting.DAO.Data;
using SoundFingerprinting.Data;
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
        private static readonly IAudioService audioService = new SoundFingerprintingAudioService(); // default audio library. 
        private EmyModelService emyModelService = EmyModelService.NewInstance("localhost", 3399); // connect to Emy on port 3399.       

        private static WaveInEvent waveSource = null;
        private static WaveFileWriter waveFile = null;
        private const int SECONDS_TO_LISTEN = 13;
        public static string tempFile = "";
        private static string lastMatchedSongId;

        public MainWindow()
        {
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
        //public static void HashTrack(string trackPathIn, TrackInfo trackInfoIn)
        //{
        //    Console.WriteLine("Hashing Track: {0}", trackInfoIn.Title);
        //    var task0 = Task.Run(async () => await StoreForLaterRetrievalAsync(trackPathIn, trackInfoIn));
        //    task0.WaitAndUnwrapException();
        //}
        private void btnHashTracks_Click(object sender, RoutedEventArgs e)
        {

        }

        //public static async Task StoreForLaterRetrievalAsync(string pathToAudioFile, TrackInfo trackInfoIn)
        //{
        //    // Connect to Emy on port 3399.
        //    var emyModelService = EmyModelService.NewInstance("localhost", 3399);

        //    //// TrackInfo from metadata.
        //    //var track = new TrackInfo("GBBKS1200164", "Theme of Tara", "KONAMI KuKeiHa CLUB", 201, metaFieldIn); // Define track info.

        //    // Create fingerprints.
        //    var hashedFingerprints = await FingerprintCommandBuilder.Instance
        //                                .BuildFingerprintCommand()
        //                                .From(pathToAudioFile)
        //                                .UsingServices(nAudioService)
        //                                .Hash();

        //    // Store hashes in the database for later retrieval.
        //    emyModelService.Insert(trackInfoIn, hashedFingerprints);
        //}
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
            }

            else if (trackToDelete == null)
            {
                MessageBox.Show(String.Format("No track exists with this ID: {0}", trackId));
            }
        }
        private void btnDeleteTrack_Click(object sender, RoutedEventArgs e)
        {
            DeleteTrack(tbxAdminInput.Text); // Confirmed to work.                                              
        }
        #endregion
    }
}