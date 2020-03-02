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
using System.Net;
using Newtonsoft.Json;
using System.Data.OleDb;
using System.IO;
using System.Diagnostics;
using System.Configuration;
using WindowsFormsAlias = System.Windows.Forms;

namespace MovieCentral
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string CONNECTION_STRING = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=|DataDirectory|\MCDB.mdb";
        private const string API_KEY = "37aec05da628be043091f9639c579d7e";
        private const string GET_CONFIG_URL = @"https://api.themoviedb.org/3/configuration?api_key={0}";
        private const string GET_MOVIEBYID_URL = @"https://api.themoviedb.org/3/movie/{0}?api_key={1}";
        private const string GET_SEARCH_URL = @"https://api.themoviedb.org/3/search/movie?query={0}&api_key={1}";
        private const string GET_MOVIE_CREDITS = @"http://api.themoviedb.org/3/movie/{0}/credits?api_key={1}";
        private string IMAGE_POSTER_BASE_URL = @"https://image.tmdb.org/t/p/w500";
        private string IMAGE_THUMBNAIL_BASE_URL = @"https://image.tmdb.org/t/p/w92";
        private string IMAGE_BACKDROP_BASE_URL = @"https://image.tmdb.org/t/p/w1280";

        private static string RED_DEEP = "#642424";
        private static string RED_LIGHT = "#E7B7B7";
        private static string BLUE_DEEP = "#102540";
        private static string BLUE_LIGHT = "#8BB4E2";
        private static string GREEN_DEEP = "#4F6328";
        private static string GREEN_LIGHT = "#D7E3BF";
        private static string GREY_DEEP = "#000000";
        private static string GREY_LIGHT = "#D9D9D9";
        private static string PURPLE_DEEP = "#3F3152";
        private static string PURPLE_LIGHT = "#CDBFD8";
        private static string ORANGE_DEEP = "#984907";
        private static string ORANGE_LIGHT = "#FCD4B1";

        List<string> movieIdListForCoverFlow = new List<string>();
        Image imgSelected = new Image();

        private string mediaFilePath = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnCloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnRight_Click(object sender, RoutedEventArgs e)
        {
            scrlView.LineRight();
        }

        private void btnLeft_Click(object sender, RoutedEventArgs e)
        {
            scrlView.LineLeft();
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            spCoverFlow.Children.Clear();

            if ((bool)chkSearchOpt.IsChecked)
            {
                var url = string.Format(GET_SEARCH_URL, txtSearchBox.Text, API_KEY);
                using (var w = new WebClient())
                {
                    try
                    {
                        string json_data = w.DownloadString(url);
                        TmdbSearch ts = JsonConvert.DeserializeObject<TmdbSearch>(json_data);

                        //Load status
                        lblStatus.Content = string.Format("{0} movies found", ts.total_results.ToString());

                        //Load coverflow
                        //movieIdListForCoverFlow.Clear();

                        //foreach (var result in ts.results)
                        //    movieIdListForCoverFlow.Add(result.id.ToString());
                        //LoadCoverFlow(movieIdListForCoverFlow);

                        foreach (var result in ts.results)
                        {
                            Image img = new Image();
                            img.Margin = new Thickness(5, 0, 5, 0);
                            img.MouseUp += new MouseButtonEventHandler(img_MouseUp);

                            //MessageBox.Show(result.id.ToString());
                            //json_data = w.DownloadString(string.Format(GET_MOVIEBYID_URL, result.id.ToString(), API_KEY));
                            //MovieDetails md = JsonConvert.DeserializeObject<MovieDetails>(new WebClient().DownloadString(string.Format(GET_MOVIEBYID_URL, result.id.ToString(), API_KEY)));
                            MovieDetails md = JsonConvert.DeserializeObject<MovieDetails>(w.DownloadString(string.Format(GET_MOVIEBYID_URL, result.id.ToString(), API_KEY)));
                            if (md.poster_path != null)
                            {
                                img.Source = new BitmapImage(new Uri(IMAGE_THUMBNAIL_BASE_URL + md.poster_path));
                            }
                            else
                            {
                                img.Source = new BitmapImage(new Uri("pack://application:,,,/Images/empty_thumbnal.png"));
                            }

                            //img.Tag = movieIdListForCoverFlow[i];
                            img.Tag = md.id.ToString();
                            spCoverFlow.Children.Add(img);
                        }

                    }
                    catch (Exception) { }
                }
            }
            else
            {
                movieIdListForCoverFlow.Clear();

                string commandString = String.Format("SELECT ID FROM MasterTable WHERE Title LIKE '%{0}%' OR [Cast] LIKE '%{0}%' OR Director LIKE '%{0}%' OR Genre LIKE '%{0}%'", txtSearchBox.Text);

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
                            movieIdListForCoverFlow.Add(dataReader["ID"].ToString());
                        }
                    }


                }
                catch (ArgumentException)
                {
                    //LoadingWindow lw = new LoadingWindow();
                    //lw.Show();
                    //lw.SetMessage("No movie found");
                }
                catch (Exception delex)
                {
                    //MessageBox.Show(delex.Message);
                }
                finally
                {
                    LoadCoverFlow(movieIdListForCoverFlow);
                    connection.Close();
                }
            }
        }

        private void LoadCoverFlow(List<string> movieIdListForCoverFlow)
        {
            OleDbConnection connection = new OleDbConnection(CONNECTION_STRING);
            try
            {

                connection.Open();

                lblStatus.Content = movieIdListForCoverFlow.Count().ToString() + " movie(s) found in collection";

                for (int i = 0; i < movieIdListForCoverFlow.Count(); i++)
                {
                    string commandString = "SELECT Thumbnail FROM MasterTable WHERE ID = '" + movieIdListForCoverFlow[i] + "';";

                    OleDbCommand command = new OleDbCommand(commandString, connection);
                    OleDbDataReader dataReader = command.ExecuteReader();

                    Image imgDb = new Image();
                    imgDb.MouseUp += new MouseButtonEventHandler(imgDb_MouseUp);
                    imgDb.Margin = new Thickness(5, 0, 5, 0);

                    if (dataReader.Read())
                    {
                        imgDb.Source = GetImageFromBinary((byte[])dataReader["Thumbnail"]);
                    }

                    imgDb.Tag = movieIdListForCoverFlow[i];

                    spCoverFlow.Children.Add(imgDb);
                }
            }
            catch (Exception)
            { }
            finally
            {
                connection.Close();
            }
        }

        //private string GetUriForCF(string movieId)
        //{
        //    var url = string.Format(GET_MOVIEBYID_URL, movieId, API_KEY);
        //    using (var w = new WebClient())
        //    {
        //        try
        //        {
        //            string json_data = w.DownloadString(url);
        //            MovieDetails md = JsonConvert.DeserializeObject<MovieDetails>(json_data);
        //            return IMAGE_THUMBNAIL_BASE_URL + md.poster_path;
        //        }
        //        catch (Exception) 
        //        {
        //            return string.Empty;
        //        }
        //    }
        //}

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            switch (ConfigurationManager.AppSettings["theme"].ToString())
            {
                case "Red":
                    SetMainWindowElements(RED_LIGHT, RED_DEEP);
                    break;

                case "Green":
                    SetMainWindowElements(GREEN_LIGHT, GREEN_DEEP);
                    break;

                case "Blue":
                    SetMainWindowElements(BLUE_LIGHT, BLUE_DEEP);
                    break;

                case "Grey":
                    SetMainWindowElements(GREY_LIGHT, GREY_DEEP);
                    break;

                case "Purple":
                    SetMainWindowElements(PURPLE_LIGHT, PURPLE_DEEP);
                    break;

                case "Orange":
                    SetMainWindowElements(ORANGE_LIGHT, ORANGE_DEEP);
                    break;
            }
            //var configUrl = string.Format(GET_CONFIG_URL, API_KEY);
            //using (var configWebClient = new WebClient())
            //{
            //    try
            //    {
            //        string json_config_data = configWebClient.DownloadString(configUrl);
            //        TmdbMain tm = JsonConvert.DeserializeObject<TmdbMain>(json_config_data);
            //        IMAGE_THUMBNAIL_BASE_URL = tm.images.base_url + tm.images.poster_sizes[0];
            //        IMAGE_POSTER_BASE_URL = tm.images.base_url + tm.images.poster_sizes[4];
            //        IMAGE_BACKDROP_BASE_URL = tm.images.base_url + tm.images.backdrop_sizes[2];
            //    }
            //    catch (Exception) { }
            //}
            btnRefresh.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        private void SetMainWindowElements(string lightColor, string deepColor)
        {
            txtTitle.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom(lightColor));
            txtTagline.Foreground = txtTitle.Foreground;
            txtRating.Foreground = txtTitle.Foreground;
            txtYear.Foreground = txtTitle.Foreground;
            txtOverview.Foreground = txtTitle.Foreground;
            tbCastLabel.Foreground = txtTitle.Foreground;
            tbDirectorLabel.Foreground = txtTitle.Foreground;
            foreach (TextBlock element in spGenre.Children)
            {
                element.Foreground = txtTitle.Foreground;
            }
            foreach (TextBlock element in wpCast.Children)
            {
                element.Foreground = txtTitle.Foreground;
            }
            foreach (TextBlock element in wpDirector.Children)
            {
                element.Foreground = txtTitle.Foreground;
            }

            rectTopLeft.Fill = txtTitle.Foreground;
            rectHeaderBar.Fill = txtTitle.Foreground;

            Background = (SolidColorBrush)(new BrushConverter().ConvertFrom(deepColor));
            txtSearchBox.Background = Background;
            rectSearchBar.Fill = Background;
            lblSearchOnline.Background = Background;
        }

        private void img_MouseUp(object sender, MouseButtonEventArgs e)
        {
            imgSelected = sender as Image;

            spGenre.Children.Clear();
            wpCast.Children.Clear();
            wpDirector.Children.Clear();

            using (var w = new WebClient())
            {
                try
                {
                    string json_data = w.DownloadString(string.Format(GET_MOVIEBYID_URL, imgSelected.Tag.ToString(), API_KEY));
                    MovieDetails md = JsonConvert.DeserializeObject<MovieDetails>(json_data);
                    txtTitle.Text = md.title;
                    txtTagline.Text = md.tagline;
                    txtRating.Text = md.vote_average.ToString();
                    txtYear.Text = md.release_date;
                    txtOverview.Text = md.overview;
                    foreach (var genre in md.genres)
                    {
                        TextBlock tbGenre = new TextBlock();
                        tbGenre.FontFamily = new FontFamily("Segoe UI Light");
                        tbGenre.FontSize = 20;
                        //tbGenre.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#8BB4E2"));
                        SetForegroundColor(tbGenre);
                        tbGenre.Text = genre.name + "   ";
                        spGenre.Children.Add(tbGenre);
                    }
                    if (md.poster_path != null)
                    {
                        imgPoster.Source = new BitmapImage(new Uri(IMAGE_POSTER_BASE_URL + md.poster_path));
                    }
                    else
                    {
                        imgPoster.Source = new BitmapImage(new Uri("pack://application:,,,/Images/empty_poster.png"));
                    }

                    if (md.backdrop_path != null)
                    {
                        imgBackdrop.Source = new BitmapImage(new Uri(IMAGE_BACKDROP_BASE_URL + md.backdrop_path));
                    }
                    else
                    {
                        imgBackdrop.Source = new BitmapImage(new Uri("pack://application:,,,/Images/empty_backdrop.png"));
                    }


                    json_data = w.DownloadString(string.Format(GET_MOVIE_CREDITS, imgSelected.Tag.ToString(), API_KEY));
                    MovieCredits mc = JsonConvert.DeserializeObject<MovieCredits>(json_data);

                    int count = 0;
                    foreach (var cast in mc.cast)
                    {
                        if (count < 20)
                        {
                            TextBlock tbCast = new TextBlock();
                            tbCast.FontFamily = new FontFamily("Segoe UI Semilight");
                            tbCast.FontSize = 14;
                            //tbCast.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#8BB4E2"));
                            SetForegroundColor(tbCast);
                            tbCast.Text = cast.name;
                            tbCast.Margin = new Thickness(0, 0, 10, 0);
                            wpCast.Children.Add(tbCast);
                            count++;
                        }
                    }
                    foreach (var director in mc.crew)
                    {
                        if (director.job == "Director")
                        {
                            TextBlock tbDir = new TextBlock();
                            tbDir.FontFamily = new FontFamily("Segoe UI Semilight");
                            tbDir.FontSize = 14;
                            //tbDir.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#8BB4E2"));
                            SetForegroundColor(tbDir);
                            tbDir.Text = director.name;
                            wpDirector.Children.Add(tbDir);
                        }
                    }
                }
                catch (Exception)
                { }
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            //string strMediaFilePath = string.Empty;

            string commandString = "INSERT INTO MasterTable VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?);";

            OleDbConnection connection = new OleDbConnection(CONNECTION_STRING);
            OleDbCommand command = new OleDbCommand(commandString, connection);

            try
            {
                connection.Open();

                command.Parameters.AddWithValue("?", imgSelected.Tag.ToString());
                command.Parameters.AddWithValue("?", txtTitle.Text);
                command.Parameters.AddWithValue("?", txtTagline.Text);
                command.Parameters.AddWithValue("?", PushIntoDBTextConverter(spGenre));
                command.Parameters.AddWithValue("?", txtYear.Text);
                command.Parameters.AddWithValue("?", PushIntoDBTextConverter(wpDirector));
                command.Parameters.AddWithValue("?", txtRating.Text);
                command.Parameters.AddWithValue("?", PushIntoDBTextConverter(wpCast));
                command.Parameters.AddWithValue("?", txtOverview.Text);
                command.Parameters.AddWithValue("?", GetBinaryFromImage(imgPoster.Source));
                command.Parameters.AddWithValue("?", GetBinaryFromImage(imgBackdrop.Source));
                command.Parameters.AddWithValue("?", GetBinaryFromImage(imgSelected.Source));
                command.Parameters.AddWithValue("?", string.Empty);

                command.ExecuteNonQuery();

                lblStatus.Content = "Movie added";
            }

            catch (OleDbException)
            {
                //LoadingWindow lw = new LoadingWindow();
                //lw.SetMessage("Movie already in collection");
                //lw.Show();
            }

            catch (Exception)
            {
                //MessageBox.Show(exp.Message);
            }
            finally
            {
                connection.Close();
            }
        }

        private void GetMediaFilePath()
        {
                WindowsFormsAlias.OpenFileDialog chooseMediaFile = new WindowsFormsAlias.OpenFileDialog();
            
                chooseMediaFile.Filter = "All Files (*.*)|*.*";
                chooseMediaFile.FilterIndex = 1;
                chooseMediaFile.Multiselect = false;
                if (chooseMediaFile.ShowDialog() == WindowsFormsAlias.DialogResult.OK)
                {
                    //MessageBox.Show(chooseMediaFile.FileName);

                    string commandString = string.Format("UPDATE MasterTable SET FilePath='{0}' WHERE ID='{1}';", "\"" + chooseMediaFile.FileName + "\"", imgSelected.Tag.ToString());

                    OleDbConnection connection = new OleDbConnection(CONNECTION_STRING);
                    OleDbCommand command = new OleDbCommand(commandString, connection);

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();

                        lblStatus.Content = "Media file path updated";
                    }

                    catch (OleDbException oex)
                    {
                        MessageBox.Show(oex.Message);
                    }

                    catch (Exception exp)
                    {
                        MessageBox.Show(exp.Message);
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
                
        }

        private byte[] GetBinaryFromImage(ImageSource imageSource)
        {
            var bmp = imageSource as BitmapImage;
            MemoryStream mem = new MemoryStream();
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            encoder.Save(mem);
            return mem.GetBuffer();
        }

        private string PushIntoDBTextConverter(WrapPanel wpDirector)
        {
            string convertedString = String.Empty;
            foreach (TextBlock tb in wpDirector.Children)
            {
                convertedString += tb.Text + "|";
            }
            return convertedString;
        }

        private string PushIntoDBTextConverter(StackPanel spGenre)
        {
            string convertedString = String.Empty;
            foreach (TextBlock tb in spGenre.Children)
            {
                convertedString += tb.Text.Trim() + "|";
            }
            return convertedString;
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            spCoverFlow.Children.Clear();
            movieIdListForCoverFlow.Clear();
            //myButton.IsChecked = true;

            string commandString = "SELECT ID FROM MasterTable;";
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
                        movieIdListForCoverFlow.Add(dataReader["ID"].ToString());
                    }
                }
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
            }
            finally
            {
                connection.Close();
                LoadCoverFlow(movieIdListForCoverFlow);
                lblStatus.Content = movieIdListForCoverFlow.Count.ToString() + " movies in collection";
            }
        }

        private BitmapImage GetImageFromBinary(byte[] bits)
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = new MemoryStream(bits);
            bitmapImage.EndInit();
            return bitmapImage;
        }

        private void imgDb_MouseUp(object sender, MouseButtonEventArgs e)
        {
            spGenre.Children.Clear();
            wpCast.Children.Clear();
            wpDirector.Children.Clear();


            imgSelected = sender as Image;
            string commandString = "SELECT Title,Tagline,Genre,Year,Director,Rating,[Cast],Overview,Poster,Backdrop,FilePath FROM MasterTable WHERE ID = '" + imgSelected.Tag.ToString() + "';";

            OleDbConnection connection = new OleDbConnection(CONNECTION_STRING);
            OleDbCommand command = new OleDbCommand(commandString, connection);

            try
            {
                connection.Open();
                OleDbDataReader dataReader = command.ExecuteReader();
                if (dataReader.Read())
                {
                    txtTitle.Text = dataReader["Title"].ToString();
                    txtTagline.Text = dataReader["Tagline"].ToString();

                    string[] strItems = dataReader["Genre"].ToString().Split('|');
                    foreach (string strItem in strItems)
                    {
                        TextBlock tb = new TextBlock();
                        tb.FontFamily = new FontFamily("Segoe UI Light");
                        tb.FontSize = 20;
                        SetForegroundColor(tb);
                        //tb.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#8BB4E2"));

                        tb.Text = strItem + "   ";

                        if (!strItem.Equals(String.Empty))
                        {
                            spGenre.Children.Add(tb);
                        }

                    }
                    txtYear.Text = dataReader["Year"].ToString();
                    strItems = dataReader["Director"].ToString().Split('|');
                    foreach (string strItem in strItems)
                    {
                        TextBlock tb = new TextBlock();
                        tb.FontFamily = new FontFamily("Segoe UI Semilight");
                        tb.FontSize = 14;
                        //tb.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#8BB4E2"));
                        SetForegroundColor(tb);
                        tb.Margin = new Thickness(0, 0, 10, 0);
                        tb.Text = strItem;
                        wpDirector.Children.Add(tb);


                    }
                    txtRating.Text = dataReader["Rating"].ToString();



                    strItems = dataReader["Cast"].ToString().Split('|');
                    foreach (string strItem in strItems)
                    {
                        TextBlock tb = new TextBlock();
                        tb.FontFamily = new FontFamily("Segoe UI Semilight");
                        tb.FontSize = 14;
                        //tb.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#8BB4E2"));
                        SetForegroundColor(tb);
                        tb.Margin = new Thickness(0, 0, 10, 0);
                        tb.Text = strItem;
                        wpCast.Children.Add(tb);
                    }
                    txtOverview.Text = dataReader["Overview"].ToString();
                    imgPoster.Source = GetImageFromBinary((byte[])dataReader["Poster"]);

                    imgBackdrop.Source = GetImageFromBinary((byte[])dataReader["Backdrop"]);

                    mediaFilePath = dataReader["FilePath"].ToString();
                }
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
            }
            finally
            {
                connection.Close();
            }
        }

        private void SetForegroundColor(TextBlock tb)
        {
            switch (ConfigurationManager.AppSettings["theme"].ToString())
            {
                case "Red":
                    tb.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom(RED_LIGHT));
                    break;

                case "Green":
                    tb.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom(GREEN_LIGHT));
                    break;

                case "Blue":
                    tb.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom(BLUE_LIGHT));
                    break;

                case "Grey":
                    tb.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom(GREY_LIGHT));
                    break;

                case "Purple":
                    tb.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom(PURPLE_LIGHT));
                    break;

                case "Orange":
                    tb.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom(ORANGE_LIGHT));
                    break;
            }
        }

        private void btnDel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string commandString = "DELETE FROM MasterTable WHERE ID='" + imgSelected.Tag.ToString() + "';";

                OleDbConnection connection = new OleDbConnection(CONNECTION_STRING);
                OleDbCommand command = new OleDbCommand(commandString, connection);

                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();

            }
            catch (NullReferenceException)
            {
                MessageBox.Show("No movie in collection");
            }
            catch (Exception delex)
            {
                MessageBox.Show(delex.Message);
            }
            finally
            {
                lblStatus.Content = "Deleted";
            }
        }

        private void btnMinimizeWindow_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settings = new SettingsWindow();
            settings.Show();
        }

        private void btnTools_Click(object sender, RoutedEventArgs e)
        {
            ScraperWindow scraper = new ScraperWindow();
            scraper.Show();
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (mediaFilePath != "")
                {
                    Process.Start("wmplayer.exe", mediaFilePath);
                }
                else
                {
                    GetMediaFilePath();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Movie file is missing");
            }
        }

        private void txtSearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnSearch_Click(this, new RoutedEventArgs());
            }
        }
    }
}
