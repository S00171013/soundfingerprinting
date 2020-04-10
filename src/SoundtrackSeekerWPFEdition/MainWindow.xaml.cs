using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using NAudio.Wave;
using SoundFingerprinting.Audio;
using SoundFingerprinting.Audio.NAudio;
using SoundFingerprinting.Emy;
using SoundFingerprinting.Builder;
using SoundFingerprinting.Data;
using SoundFingerprinting.DAO.Data;
using Newtonsoft.Json;
using System.Windows.Media.Imaging;

namespace SoundtrackSeekerWPFEdition
{
    public partial class MainWindow : Window
    {
        private static EmyModelService emyModelService = EmyModelService.NewInstance("localhost", 3399); // connect to Emy on port 3399. 
        private static readonly IAudioService audioService = new SoundFingerprintingAudioService(); // default audio library. Used to discover matches for queries.
        private static readonly IAudioService nAudioService = new NAudioService(); // Additional NAudio library. Used to hash both WAV and MP3 files.   
        private static HttpClient client = new HttpClient();

        private static WaveInEvent waveSource = null;
        private static WaveFileWriter waveFile = null;
        private const int SECONDS_TO_LISTEN = 7; // I think 7 seconds will be a good interval to listen for.
        private static string tempFile = "";

        public MainWindow()
        {
            SetUpClient();
            InitializeComponent();
        }

        private async void btnApiTest_Click(object sender, RoutedEventArgs e)
        {
            SearchAlbumImage("tekken");
            //string albumLink = null;
            //string albumUrl = null;
            //// Call asynchronous network methods in a try/catch block to handle exceptions.
            //// https://stackoverflow.com/questions/6620165/how-can-i-parse-json-with-c                
            //string responseBody = await client.GetStringAsync("\"tekken\"");
            //dynamic searchInfo = JsonConvert.DeserializeObject(responseBody);
            //try
            //{
            //    albumLink = searchInfo.results.albums[0].link;
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("Unable to find any album results.");
            //}

            //if (albumLink != null)
            //{
            //    albumUrl = string.Format("https://vgmdb.info/{0}", albumLink);
            //    string albumResponseBody = await client.GetStringAsync(albumUrl);
            //    dynamic albumInfo = JsonConvert.DeserializeObject(albumResponseBody);
            //    string imageUrl = albumInfo.picture_small.ToString();

            //    var image = new Image(); // https://stackoverflow.com/questions/18435829/showing-image-in-wpf-using-the-url-link-from-database                
            //    BitmapImage bitmap = new BitmapImage();
            //    bitmap.BeginInit();
            //    bitmap.UriSource = new Uri(imageUrl, UriKind.Absolute);
            //    bitmap.EndInit();

            //    imgAlbum.Source = bitmap;
            //}
        }

        private async void SearchAlbumImage(string albumToSearch)
        {
            string albumLink = null;
            // Call asynchronous network methods in a try/catch block to handle exceptions.
            // https://stackoverflow.com/questions/6620165/how-can-i-parse-json-with-c                            
            dynamic searchInfo = await RetrieveObjectFromClient(string.Format("search/albums/\"{0}\"", albumToSearch));

            try
            {
                albumLink = searchInfo.results.albums[0].link;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to find any album results.");
            }

            if (albumLink != null)
            {                
                dynamic albumInfo = await RetrieveObjectFromClient(albumLink);

                string imageUrl = albumInfo.picture_small.ToString();
                DisplayAlbumImage(imageUrl);
            }
        }

        private void DisplayAlbumImage(string imageUrl)
        {
            var image = new Image(); // https://stackoverflow.com/questions/18435829/showing-image-in-wpf-using-the-url-link-from-database                
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imageUrl, UriKind.Absolute);
            bitmap.EndInit();

            imgAlbum.Source = bitmap;
        }

        private async Task<dynamic> RetrieveObjectFromClient(string urlPath)
        {
            string responseBody = await client.GetStringAsync(urlPath);
            dynamic jsonInfo = JsonConvert.DeserializeObject(responseBody);
            return jsonInfo;
        }

        private static void SetUpClient()
        {
            client.BaseAddress = new Uri("https://vgmdb.info/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        #region TRACK MATCHING METHODS
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
        private void waveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            waveFile.Write(e.Buffer, 0, e.BytesRecorded);
        }
        private async Task CeaseListening(WaveInEvent waveSource, WaveFileWriter waveFile, int seconds)
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

                    SearchAlbumImage(foundAlbum); // Gotta try this tomorrow.
                }), DispatcherPriority.Render);
            }

            await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                HandleVisibility(lblListenMessage, "HIDE");
                HandleVisibility(btnSeek, "DISPLAY");

            }), DispatcherPriority.Render);
        }
        private async Task<TrackData> GetBestMatchForSong(string queryAudioFile)
        {
            int secondsToAnalyze = SECONDS_TO_LISTEN; // number of seconds to analyze from query file.           
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
        #endregion

        // ADMIN Methods
        #region TRACK HASHING METHODS
        private async void btnHashTracks_Click(object sender, RoutedEventArgs e)
        {
            await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                HandleVisibility(btnHashTracks, "HIDE");
                HandleVisibility(lblHashingMessage, "DISPLAY");
            }), DispatcherPriority.Render);

            List<string> filesToHash = new List<string>();

            try
            {
                // Create an enumerable list of all MP3s found in the given directory (including subdirectories).
                filesToHash = Directory.EnumerateFiles(tbxAdminInput.Text, "*.mp3", SearchOption.AllDirectories).ToList();

                foreach (string trackFilePath in filesToHash)
                { // For each MP3 track found...
                    TrackInfo newTrackInfo = ExtractTrackInfo(trackFilePath); // ...get track info.

                    await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        HashTrack(trackFilePath, newTrackInfo); // Hash the track.
                    }), DispatcherPriority.Render);
                }

                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    HandleVisibility(lblHashingMessage, "HIDE");
                    HandleVisibility(btnHashTracks, "DISPLAY");
                }), DispatcherPriority.Render);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    HandleVisibility(lblHashingMessage, "HIDE");
                    HandleVisibility(btnHashTracks, "DISPLAY");
                }), DispatcherPriority.Render);
            }
        }
        private TrackInfo ExtractTrackInfo(string trackFilePathIn)
        {
            // https://stackoverflow.com/questions/29125336/error-with-ultraid3lib-vb // UltraID3Lib does not support ID3V2.4, seems we need to use "TagLib#" instead.
            // https://stackoverflow.com/questions/4361587/where-can-i-find-tag-lib-sharp-examples
            TagLib.File f = TagLib.File.Create(trackFilePathIn); // Create a TagLib file from the MP3's path.                       

            // Metafield setup.
            Dictionary<string, string> newTrackMetaField = new Dictionary<string, string>();
            newTrackMetaField.Add("File Name", f.Name);
            newTrackMetaField.Add("Track Number", f.Tag.Track.ToString());
            newTrackMetaField.Add("Album", f.Tag.Album);
            newTrackMetaField.Add("Genre", f.Tag.JoinedGenres);
            newTrackMetaField.Add("Year", f.Tag.Year.ToString());
            newTrackMetaField.Add("Album Artist", f.Tag.JoinedAlbumArtists);
            newTrackMetaField.Add("Duration", Math.Round(f.Properties.Duration.TotalSeconds, 2).ToString());
            newTrackMetaField.Add("Bit Rate", f.Properties.AudioBitrate.ToString());
            newTrackMetaField.Add("Sample Rate", f.Properties.AudioSampleRate.ToString());

            TrackInfo extractedTrackInfo = new TrackInfo(Guid.NewGuid().ToString(), f.Tag.Title, f.Tag.JoinedPerformers, newTrackMetaField, MediaType.Audio);
            return extractedTrackInfo;
        }
        private void HashTrack(string trackPathIn, TrackInfo trackInfoIn)
        {
            lbxOutput.Items.Add(String.Format("Hashing Track: '{0}' by {1}.", trackInfoIn.Title, trackInfoIn.Artist));

            var task = Task.Run(async () => await StoreForLaterRetrievalAsync(trackPathIn, trackInfoIn));
            task.Wait(); // WaitAndUnwrapException in the console version. May have to fix something here.            
        }
        private async Task StoreForLaterRetrievalAsync(string pathToAudioFile, TrackInfo trackInfoIn)
        {
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

        #region TRACK HASH DELETION METHODS
        private async void btnDeleteTrack_Click(object sender, RoutedEventArgs e)
        {
            await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                HandleVisibility(btnDeleteTrack, "HIDE");
                HandleVisibility(lblDeletingMessage, "DISPLAY");
            }), DispatcherPriority.Render);

            DeleteTrack(tbxAdminInput.Text);
        }
        private async void DeleteTrack(string trackId)
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

                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    HandleVisibility(lblDeletingMessage, "HIDE");
                    HandleVisibility(btnDeleteTrack, "DISPLAY");
                }), DispatcherPriority.Render);
            }

            else if (trackToDelete == null)
            {
                MessageBox.Show(String.Format("No track exists with this ID: {0}", trackId));

                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    HandleVisibility(lblDeletingMessage, "HIDE");
                    HandleVisibility(btnDeleteTrack, "DISPLAY");
                }), DispatcherPriority.Render);
            }
        }
        #endregion

        #region UI ELEMENT VISIBILITY MANIPULATION METHODS
        private void HandleVisibility(ContentControl uiElement, string action)
        {
            if (action.ToUpper() == "DISPLAY") uiElement.Visibility = Visibility.Visible;
            else if (action.ToUpper() == "HIDE") uiElement.Visibility = Visibility.Hidden;
            else if (action.ToUpper() == "COLLAPSE") uiElement.Visibility = Visibility.Collapsed;
        }
        private void SetTrackInfoVisibility(string action)
        {
            HandleVisibility(lblTitle, action);
            HandleVisibility(lblAlbum, action);
            HandleVisibility(lblArtist, action);
        }
        #endregion        
    }
}