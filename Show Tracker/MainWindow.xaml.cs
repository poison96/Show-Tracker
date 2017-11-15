using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using RestSharp;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using RestSharp.Deserializers;

namespace Show_Tracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ArrayList showNames;
        List<TvShow> shows;
        RestClient client;
        TvShow selectedShow;

        string api_base_url = "https://api.themoviedb.org";
        string api_tv_search_parameter = "/3/search/tv";
        string api_image_url = "https://image.tmdb.org/t/p/w500";
        string api_key = "?api_key=470f2540ae00b90018e8f4c0550e1823&";

        public MainWindow()
        {
            InitializeComponent();

            // shows stores all shows names
            showNames = new ArrayList();

            // create list of shows
            shows = new List<TvShow>();

            // Load Data from file
            LoadData();

            // Bind shows to listbox
            showList.ItemsSource = shows;


        }

        //Loads a list of tvshows from a json file
        private void LoadData()
        {
            //Gets directory where file is located
            var directory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            //If file exists, open and read the file and deserialize JSON into shows
            if (File.Exists(System.IO.Path.Combine(directory, "ShowTracker", "data.json")))
            {
                using (StreamReader file = File.OpenText(System.IO.Path.Combine(directory, "ShowTracker", "data.json")))
                {
                    var jsonString = file.ReadToEnd();
                    shows = JsonConvert.DeserializeObject<List<TvShow>>(jsonString);
                }
            }
        }

        //Saves a list of tvshows to a json file
        private void SaveData()
        {
            //Gets directory where file will be saved
            var directory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            //If directory doesn't exist, create it
            Directory.CreateDirectory(System.IO.Path.Combine(directory, "ShowTracker"));

            using (StreamWriter file = File.CreateText(System.IO.Path.Combine(directory, "ShowTracker", "data.json")))
            {
                JsonSerializer serializer = new JsonSerializer();
                //serialize object directly into file stream
                serializer.Serialize(file, shows);
            }
        }

        private async void AddShowButton_Click(object sender, RoutedEventArgs e)
        {
            // Get TvShow from themoviedb api from search text asynchronously 
            TvShow show = await GetTvShowAsync(addShowText.Text);
            // Add show  to show list
            shows.Add(show);

            // Update the ListBox
            showList.Items.Refresh();
            //Set the selected item in listbox
            showList.SelectedItem = show;

            //Clear text in addbox
            addShowText.Text = "";


        }

        private async Task<TvShow> GetTvShowAsync(string query)
        {
            // Complete url for querying api
            string searchURL = Uri.EscapeUriString(api_base_url + api_tv_search_parameter + api_key + "query=" + query);
            //Create HttpClient to do the API calls
            client = new RestClient(searchURL);
            // Get response from API
            var response = await client.ExecuteGetTaskAsync<TvShowResultsQuery>(new RestRequest());
            // return the first tv show result
            return JsonConvert.DeserializeObject<TvShowResultsQuery>(response.Content).results[0];
        }

        private void showList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //When the selection is change for showList, Update all the fields for displaying information about the tv show
            if(shows.Count >= 1)
            {
                selectedShow = showList.SelectedItem as TvShow;

                NameText.Text = selectedShow.Name;
                FirstAirDate.Text = selectedShow.First_air_date;
                DescriptionText.Text = selectedShow.Overview;
                SeasonText.Text = selectedShow.CurrentSeason.ToString();
                EpisodeText.Text = selectedShow.CurrentEpisode.ToString();

                showPoster.Source = new BitmapImage(new Uri(api_image_url + selectedShow.Poster_path));
            }
            else // Or clear all information when there is nothing to select
            {
                NameText.Text = "No TV Show Selected";
                FirstAirDate.Text = "";
                DescriptionText.Text = "";
                SeasonText.Text = "";
                EpisodeText.Text = "";
                showPoster.Source = null;
            }
            
        }

        // Ability to press Enter while in textbox to add tvshow
        private void addShowText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddShowButton_Click(this, new RoutedEventArgs());
            }
        }

        //Updates the season you are up to
        private void SeasonText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(selectedShow != null)
            {
                int parsedInt;
                if(Int32.TryParse(SeasonText.Text, out parsedInt))
                {
                    selectedShow.CurrentSeason = parsedInt;
                }  
            }           
        }

        //Prevent anything other than numbers to be entered
        private void SeasonText_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, e.Text.Length - 1))
                e.Handled = true;
        }

        //Updates the epsiode you are up to
        private void EpisodeText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (selectedShow != null)
            {
                int parsedInt;
                if (Int32.TryParse(EpisodeText.Text, out parsedInt))
                {
                    selectedShow.CurrentEpisode = parsedInt;
                }
            }
        }

        //Prevent anything other than numbers to be entered
        private void EpisodeText_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, e.Text.Length - 1))
                e.Handled = true;
        }

        // Remove show from list and shows list
        private void removeShowButton_Click(object sender, RoutedEventArgs e)
        {
            TvShow tempShow = showList.SelectedItem as TvShow;
            if(shows.Count >= 1)
            { 
                selectedShow = shows[0];
                showList.SelectedItem = shows[0];
                shows.Remove(tempShow);
                showList.Items.Refresh();
            }
        }

        // When program closes, save the data
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveData();
        }
    }

    //Class for the response from the API
    public class TvShowResultsQuery
    {
        public TvShowResultsQuery()
        {
        }

        public int Page { get; set; }
        public int Total_results { get; set; }
        public int Total_pages { get; set; }
        public List<TvShow> results { get; set; } 
    }

    //Class that stores all the information required about a tv show
    public class TvShow
    {
        public TvShow()
        {
        }

        public string Original_name { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public int Vote_count { get; set; }
        public double Vote_average { get; set; }
        public string Poster_path { get; set; }
        public string First_air_date { get; set; }
        public double Popularity { get; set; }
        public List<int> Genre_ids { get; set; }
        public string Original_language { get; set; }
        public string Backdrop_path { get; set; }
        public string Overview { get; set; }
        public List<string> Origin_country { get; set; }
        public int CurrentSeason { get; set; }
        public int CurrentEpisode { get; set; }
    }


}
