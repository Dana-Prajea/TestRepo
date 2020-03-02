using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Data.OleDb;
using System.Net;
using Newtonsoft.Json;
using System.Drawing;
using System.Drawing.Imaging;


namespace MovieCentral
{
    /// <summary>
    /// Interaction logic for ScraperWindow.xaml
    /// </summary>
    public partial class ScraperWindow : Window
    {
        private static string CONNECTION_STRING = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=MCDB.mdb";
        private const string API_KEY = "37aec05da628be043091f9639c579d7e";
        private const string GET_MOVIEBYID_URL = @"https://api.themoviedb.org/3/movie/{0}?api_key={1}";
        private const string GET_SEARCH_URL = @"https://api.themoviedb.org/3/search/movie?query={0}&year={1}&api_key={2}";
        private const string GET_MOVIE_CREDITS = @"http://api.themoviedb.org/3/movie/{0}/credits?api_key={1}";
        private string IMAGE_POSTER_BASE_URL = @"https://image.tmdb.org/t/p/w500";
        private string IMAGE_THUMBNAIL_BASE_URL = @"https://image.tmdb.org/t/p/w92";
        private string IMAGE_BACKDROP_BASE_URL = @"https://image.tmdb.org/t/p/w1280";
        private static string RED_DEEP = "#642424";
        private static string GREEN_DEEP = "#4F6328";

        private BackgroundWorker backgroundWorker;

        List<string> failedMovies = new List<string>();
        List<string> movieIdInDB = new List<string>();
        private string movieFolderPath = string.Empty;


        #region UtilityMethods




        /// <summary>
        /// Cleans the movie name read from directory
        /// for more accurate search results from tmdb
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private string[] GetCleanSearchString(string s)
        {
            string year = string.Empty;

            s = s.Replace('.', '+').Replace(' ', '+');

            if (s.Contains('['))
            {
                s = s.Substring(0, s.IndexOf(']'));
                //s = s.Substring(0, s.IndexOf("[")) + "+" + Regex.Match(s, @"[0-9][0-9][0-9][0-9]", RegexOptions.IgnoreCase).Value;
                year = Regex.Match(s, @"[0-9][0-9][0-9][0-9]", RegexOptions.IgnoreCase).Value;
                s = s.Substring(0, s.IndexOf("["));
            }

            if (s.Contains('('))
            {
                s = s.Substring(0, s.IndexOf(')'));
                //s = s.Substring(0, s.IndexOf("(")) + "+" + Regex.Match(s, @"[0-9][0-9][0-9][0-9]", RegexOptions.IgnoreCase).Value;
                year = Regex.Match(s, @"[0-9][0-9][0-9][0-9]", RegexOptions.IgnoreCase).Value;
                s = s.Substring(0, s.IndexOf("("));
            }

            if (s.Contains('{'))
            {
                s = s.Substring(0, s.IndexOf('}'));
                //s = s.Substring(0, s.IndexOf("{")) + "+" + Regex.Match(s, @"[0-9][0-9][0-9][0-9]", RegexOptions.IgnoreCase).Value;
                year = Regex.Match(s, @"[0-9][0-9][0-9][0-9]", RegexOptions.IgnoreCase).Value;
                s = s.Substring(0, s.IndexOf("{"));
            }

            s = s.Replace("+-+", "+").Replace("++", "+");

            string[] removables = { "DVDrip", "HD", "BRRip", "x264", "H264", "BluRay", "720p", "XviD", "480p", "DVDSCR", "DVDRip", "DvDRiP",
                                  "720P", "DvdRip", "1080p" };

            foreach (string str in removables)
            {
                if (s.Contains(str))
                    s = s.Substring(0, s.IndexOf(str));
            }

            s = s.TrimEnd('+');

            //return s;
            string[] cleanedQuery = new string[] { s, year };
            return cleanedQuery;
        }

        private void GetMovieIdsInDB()
        {
            movieIdInDB.Clear();

            string commandString = String.Format("SELECT ID FROM MasterTable");

            OleDbConnection connection = new OleDbConnection(CONNECTION_STRING);
            OleDbCommand command = new OleDbCommand(commandString, connection);

            try
            {
                connection.Open();
                OleDbDataReader dataReader = command.ExecuteReader();

                if (dataReader != null)
                {
                    while (dataReader.Read())
                    {
                        movieIdInDB.Add(dataReader["ID"].ToString());
                    }
                }
            }
            catch
            { }
            finally
            {
                connection.Close();
                lblStatus.Content = movieIdInDB.Count.ToString() + " movies in collection.";
            }
        }

        /// <summary>
        /// Verifies if the scanned movie is present in library
        /// Enables search recursively, skipping addition of movie already in collection
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private bool CheckIfMovieExistsInDB(string s)
        {
            if (movieIdInDB.Contains(s))
                return true;
            else
                return false;
        }

        #endregion


        public ScraperWindow()
        {
            InitializeComponent();
            backgroundWorker = new BackgroundWorker();
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorker_DoWork);
            backgroundWorker.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker_ProgressChanged);
            backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker_RunWorkerCompleted);
        }



        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            GetMovieIdsInDB();
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog().ToString().Equals("OK"))
                tbFolderPath.Text = dialog.SelectedPath;
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            GetMovieIdsInDB();

            try
            {
                string[] filepaths = Directory.GetDirectories(tbFolderPath.Text);
                movieFolderPath = tbFolderPath.Text;

                wpResults.Children.Clear();
                foreach (string s in filepaths)
                {
                    TextBlock tb = new TextBlock();
                    tb.FontFamily = new System.Windows.Media.FontFamily("Segoe UI Semilight");
                    tb.FontSize = 12;
                    tb.Margin = new Thickness(5, 5, 0, 0);
                    tb.Foreground = System.Windows.Media.Brushes.White;
                    tb.Text = s.Replace(tbFolderPath.Text, "").Replace("\\", "");
                    wpResults.Children.Add(tb);
                }

                progStatus.Visibility = Visibility.Visible;
                progStatus.Minimum = 0;
                progStatus.Maximum = wpResults.Children.Count;
                progStatus.Value = 0;

                lblStatus.Content = "Downloading movie info. Please be patient.";

                string folderList = String.Empty;

                foreach (TextBlock tb in wpResults.Children)
                {
                    folderList += tb.Text + "|";
                }

                backgroundWorker.RunWorkerAsync(folderList);
            }
            catch (DirectoryNotFoundException)
            { }
        }

        private void btnMinimizeWindow_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btnCloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string[] folderList = (e.Argument.ToString()).Split('|');

            WebClient webClient = new WebClient();
            //XDocument xmlDoc = new XDocument();
            TmdbSearch ts = new TmdbSearch();

            for (int i = 0; i < folderList.Length - 1; i++)
            {
                try
                {
                    //xmlDoc = XDocument.Parse(System.Text.Encoding.UTF8.GetString(webClient.DownloadData(string.Format(MOVIE_SEARCH_URL, API_KEY, GetCleanSearchString(folderList[i])))));
                    string[] cleanedQuery = GetCleanSearchString(folderList[i]);
                    string json_data = webClient.DownloadString(string.Format(GET_SEARCH_URL, cleanedQuery[0], cleanedQuery[1], API_KEY));
                    ts = JsonConvert.DeserializeObject<TmdbSearch>(json_data);
                }
                catch (WebException)
                {
                    //Do nothing
                }
                catch (Exception exp)
                {
                    System.Windows.MessageBox.Show(exp.Message);
                }



                //If there is nothing in search result, add to failedMovies list
                //if (xmlDoc.Descendants("movie").Count().Equals(0))
                //{
                //    failedMovies.Add(folderList[i]);
                //}
                if (ts.total_results == 0)
                {
                    failedMovies.Add(folderList[i]);
                }

                else
                {
                    //If movie is already in DB, skip next steps
                    if (CheckIfMovieExistsInDB(ts.results[0].id.ToString()))
                    {
                        continue;
                    }

                    //New movie info is downloaded and added to collection
                    else
                    {
                        string currentID = ts.results[0].id.ToString();
                        string strTitle = String.Empty;
                        string strTagline = String.Empty;
                        string strReleaseDate = String.Empty;

                        string strOverview = String.Empty;
                        string strRating = String.Empty;

                        string strDirector = String.Empty;
                        string strCast = String.Empty;
                        string strGenre = String.Empty;

                        string strThumbnail = String.Empty;
                        string strPoster = String.Empty;
                        string strBackdrop = String.Empty;

                        string strMediaFilePath = String.Empty;

                        //Get the media file paths
                        string[] filePaths = Directory.GetFiles(movieFolderPath + "\\" + folderList[i]);
                        foreach (string filePath in filePaths)
                        {
                            if ((filePath.ToLower().Contains(".wmv")
                                || filePath.ToLower().Contains(".mp4")
                                || filePath.ToLower().Contains(".avi")
                                || filePath.ToLower().Contains(".mkv")
                                || filePath.ToLower().Contains(".vob")
                                || filePath.ToLower().Contains(".m2ts")
                                || filePath.ToLower().Contains(".m4v")) && !filePath.ToLower().Contains("sample"))
                            {
                                //Process.Start("wmplayer.exe", "\"" + filePath + "\"");
                                strMediaFilePath = "\"" + filePath + "\"";
                            }
                        }



                        #region FetchFromTMDB

                        //xmlDoc = XDocument.Parse(System.Text.Encoding.UTF8.GetString(webClient.DownloadData(string.Format(MOVIE_INFO_URL, API_KEY, currentID))));
                        MovieDetails md = JsonConvert.DeserializeObject<MovieDetails>(new WebClient().DownloadString(string.Format(GET_MOVIEBYID_URL, currentID, API_KEY)));


                        strTitle = md.title;
                        strTagline = md.tagline;
                        strReleaseDate = md.release_date;
                        strOverview = md.overview;
                        strRating = md.vote_average.ToString();

                        foreach (var genre in md.genres)
                        {
                            strGenre += genre.name + "|";
                        }

                        MovieCredits mc = JsonConvert.DeserializeObject<MovieCredits>(new WebClient().DownloadString(string.Format(GET_MOVIE_CREDITS, currentID, API_KEY)));

                        int count = 0;
                        foreach (var cast in mc.cast)
                        {
                            if (count < 20)
                            {
                                strCast += cast.name + "|";
                                count++;
                            }
                        }

                        foreach (var director in mc.crew)
                        {
                            if (director.job == "Director")
                            {
                                strDirector += director.name + "|";
                            }
                        }


                        try
                        {
                            strBackdrop = IMAGE_BACKDROP_BASE_URL + md.backdrop_path;
                        }
                        catch
                        {
                            strBackdrop = "./Images/empty_backdrop.png";
                        }

                        try
                        {
                            strPoster = IMAGE_POSTER_BASE_URL + md.poster_path;
                        }
                        catch
                        {
                            strPoster = "./Images/empty_poster.png";
                        }

                        try
                        {
                            strThumbnail = IMAGE_THUMBNAIL_BASE_URL + md.poster_path;
                        }
                        catch
                        {
                            strThumbnail = "./Images/empty_thumbnal.png";
                        }

                        #endregion

                        #region PushInDB


                        string commandString = "INSERT INTO MasterTable VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?);";

                        OleDbConnection connection = new OleDbConnection(CONNECTION_STRING);
                        OleDbCommand command = new OleDbCommand(commandString, connection);

                        try
                        {
                            connection.Open();

                            //Dummy object to load images from DB
                            PictureBox pic = new PictureBox();

                            pic.Load(strPoster);
                            System.Drawing.Image poster = pic.Image;
                            MemoryStream msPoster = new MemoryStream();
                            poster.Save(msPoster, ImageFormat.Jpeg);
                            byte[] posterBuff = new byte[msPoster.Length];
                            msPoster.Position = 0;
                            msPoster.Read(posterBuff, 0, posterBuff.Length);


                            pic.Load(strBackdrop);
                            System.Drawing.Image backdrop = pic.Image;
                            MemoryStream msBackdrop = new MemoryStream();
                            backdrop.Save(msBackdrop, ImageFormat.Jpeg);
                            byte[] backdropBuff = new byte[msBackdrop.Length];
                            msBackdrop.Position = 0;
                            msBackdrop.Read(backdropBuff, 0, backdropBuff.Length);


                            pic.Load(strThumbnail);
                            System.Drawing.Image thumbnail = pic.Image;
                            MemoryStream msThumbnail = new MemoryStream();
                            thumbnail.Save(msThumbnail, ImageFormat.Jpeg);
                            byte[] thumbnailBuff = new byte[msThumbnail.Length];
                            msThumbnail.Position = 0;
                            msThumbnail.Read(thumbnailBuff, 0, thumbnailBuff.Length);


                            command.Parameters.AddWithValue("?", currentID);
                            command.Parameters.AddWithValue("?", (strTitle).Replace('\'', '_'));
                            command.Parameters.AddWithValue("?", strTagline);
                            command.Parameters.AddWithValue("?", strGenre);
                            command.Parameters.AddWithValue("?", strReleaseDate);
                            command.Parameters.AddWithValue("?", strDirector);
                            command.Parameters.AddWithValue("?", strRating);
                            command.Parameters.AddWithValue("?", strCast);
                            command.Parameters.AddWithValue("?", strOverview);
                            command.Parameters.AddWithValue("?", posterBuff);
                            command.Parameters.AddWithValue("?", backdropBuff);
                            command.Parameters.AddWithValue("?", thumbnailBuff);
                            command.Parameters.AddWithValue("?", strMediaFilePath);

                            command.ExecuteNonQuery();

                        }

                        catch (OleDbException)
                        {
                            failedMovies.Add(folderList[i]);
                        }

                        catch (Exception exp)
                        {
                            System.Windows.MessageBox.Show(exp.Message);
                        }
                        finally
                        {
                            connection.Close();
                        }


                        #endregion
                    }
                }

                backgroundWorker.ReportProgress(i + 1);
            }
        }

        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progStatus.Value = e.ProgressPercentage;
            lblStatus.Content = (e.ProgressPercentage - failedMovies.Count).ToString() + " movie info processed.";
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            lblStatus.Content = "Scraping complete. Please close this window and click REFRESH on Movie Central.";

            progStatus.Visibility = Visibility.Hidden;



            //Paints movie names in wrap panel
            //Green is info found and added
            //Red if fails
            foreach (TextBlock tb in wpResults.Children)
            {
                tb.FontFamily = new System.Windows.Media.FontFamily("Segoe UI Semilight");
                tb.FontSize = 12;
                tb.Margin = new Thickness(5, 5, 0, 0);
                tb.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom(GREEN_DEEP));
                foreach (string s in failedMovies)
                {
                    if (tb.Text.Equals(s))
                    {
                        tb.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom(RED_DEEP));
                        break;
                    }
                }
            }



            //failed movie names are stored in text file
            //for end-user access
            try
            {
                TextWriter tw = new StreamWriter("ScrapingFailedMovies.txt");
                foreach (string s in failedMovies)
                {
                    tw.WriteLine(s);
                }
                tw.Close();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
            }
        }






    }
}
