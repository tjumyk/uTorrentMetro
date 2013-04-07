using Fizzler;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;
using System.Reflection;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace utorrentMetro
{
    public class UtorrentClient
    {
        public static readonly HttpClient client = new HttpClient();

        // URL Query Entries
        public static readonly String GET_TOKEN_ENTRY = "/gui/token.html";
        public static readonly String GET_SETTINGS_ENTRY = "/gui/?action=getsettings";
        public static readonly String GET_TRANS_HIST_ENTRY = "/gui/?action=getxferhist";
        public static readonly String GET_HEART_BEAT_ENTRY = "/gui/?list=1&getmsg=1";
        public static readonly String GET_FILE_LIST_ENTRY = "/gui/?action=getfiles&list=1&getmsg=1";
        public static readonly String GET_FILE_DOWNLOAD_ENTRY = "/proxy?disposition=ATTACHMENT&service=DOWNLOAD&qos=0";
        
        // Update Settings
        public static readonly int UPDATE_INTERVAL = 1000;

        // State codes
        public static readonly int STATE_STARTED = 1;
        public static readonly int STATE_CHECKING = 2;
        public static readonly int STATE_ERROR = 16;
        public static readonly int STATE_PAUSED = 32;
        public static readonly int STATE_QUEUED = 64;

        // Torrent commands
        public static readonly String TORRENT_CMD_START = "start";
        public static readonly String TORRENT_CMD_PAUSE = "pause";
        public static readonly String TORRENT_CMD_UNPAUSE = "unpause";
        public static readonly String TORRENT_CMD_FORCE_START = "forcestart";
        public static readonly String TORRENT_CMD_STOP = "stop";
        public static readonly String TORRENT_CMD_REMOVE = "remove";
        public static readonly String TORRENT_CMD_REMOVE_TORRENT = "removetorrent";
        public static readonly String TORRENT_CMD_REMOVE_DATA = "removedata";
        public static readonly String TORRENT_CMD_REMOVE_DATA_TORRENT = "removedatatorrent";

        // Login/Logout params
        public String hostAddress, port;
        public String username, password;
        public bool isLoggedIn;
        
        // Token and Cache
        public String token;
        public String cacheID;

        // Current UI-Visible resourses
        public ObservableCollection<Torrent> currentTorrentList = new ObservableCollection<Torrent>();
        public ObservableCollection<RssFeed> currentRssFeedList = new ObservableCollection<RssFeed>();
        
        public async Task login() {
            try
            {
                //Check hostAddress
                if (!hostAddress.StartsWith("http://") && !hostAddress.StartsWith("https://")) {
                    hostAddress = "http://" + hostAddress;
                }
                
                //Set authorization info to HttpClient
                byte[] cred = UTF8Encoding.UTF8.GetBytes(username+":"+password);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(cred));

                //Get utorrent webui token
                isLoggedIn = false;
                String resp = await client.GetStringAsync(hostAddress + ":" + port + GET_TOKEN_ENTRY +"?t="+Util.getTime());
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(resp);
                HtmlNode root = doc.DocumentNode;
                token = root.QuerySelectorAll("#token").First<HtmlNode>().InnerText;
                if (String.IsNullOrWhiteSpace(token))
                {
                    throw new UTException("Invalid access to uTorrent webUI module. Please check params to log in.");
                }
                else
                {
                    isLoggedIn = true;
                }
            }
            catch (Exception e) {
                throw new UTException("Can not initialize connection to uTorrent webUI module. Please check params to log in.",e);
            }
        }

        public async Task logout() {
            isLoggedIn = false;
            await Task.Delay((int)(UPDATE_INTERVAL*1.1));
            currentRssFeedList.Clear();
            currentTorrentList.Clear();
        }

        public async void startUpdate(bool needInitData) {
            try
            {
                if (needInitData)
                {
                    InitData data = await getInitData();
                    currentTorrentList.Clear();
                    foreach (Torrent tor in data.torrents)
                    {
                        currentTorrentList.Add(tor);
                        Util.log("Add Init Torrent: " + tor);
                    }
                    currentRssFeedList.Clear();
                    foreach (RssFeed rf in data.rssfeeds)
                    {
                        currentRssFeedList.Add(rf);
                        Util.log("Add Init RssFeed: " + rf);
                    }
                }

                while (!App.stopBackgroundTask && isLoggedIn && !MainPage.cancelUpdate)
                {
                    await Task.Delay(UPDATE_INTERVAL);
                    Util.log("Refreshing...");
                    HeartBeatInfo info = await getHeartBeat();
                    UpdateInfo(info);
                }
            }
            catch (Exception e) { // this function runs in background, so proper error-handling is needed here.
                Util.sendWarningToast(e.Message);
            }
        }

        private void UpdateInfo(HeartBeatInfo info)
        {
            foreach (Torrent newTor in info.torrentUpdated)
            {
                Torrent oldTor = null;
                foreach (Torrent item in currentTorrentList)
                {
                    if (item.Hash == newTor.Hash)
                    {
                        oldTor = item;
                        break;
                    }
                }
                if (oldTor == null)
                {
                    currentTorrentList.Add(newTor);
                    Util.log("Add New Torrent: " + newTor);
                }
                else
                {
                    Util.update(oldTor, newTor);
                    Util.log("Update Torrent: " + oldTor);
                }
            }
            foreach (String toRemoveTorHash in info.torrentRemoved)
            {
                Torrent oldTor = null;
                foreach (Torrent item in currentTorrentList)
                {
                    if (item.Hash == toRemoveTorHash)
                    {
                        oldTor = item;
                        break;
                    }
                }
                if (oldTor != null)
                {
                    oldTor.ToBeRemoved = true;
                    currentTorrentList.Remove(oldTor);
                    Util.log("Remove Torrent: " + oldTor);
                }
            }

            foreach (RssFeed newFeed in info.rssFeedUpdated)
            {
                RssFeed oldFeed = null;
                foreach (RssFeed item in currentRssFeedList)
                {
                    if (item.ID == newFeed.ID)
                    {
                        oldFeed = item;
                        break;
                    }
                }
                if (oldFeed == null)
                {
                    currentRssFeedList.Add(newFeed);
                    Util.log("Add New RssFeed: " + newFeed);
                }
                else
                {
                    Util.update(oldFeed, newFeed);
                    Util.log("Update RssFeed: " + oldFeed);
                }
            }

            foreach (String toRemoveFeedID in info.rssFeedRemoved)
            {
                RssFeed oldFeed = null;
                foreach (RssFeed item in currentRssFeedList)
                {
                    if (item.ID == toRemoveFeedID)
                    {
                        oldFeed = item;
                        break;
                    }
                }
                if (oldFeed != null)
                {
                    oldFeed.ToBeRemoved = true;
                    currentRssFeedList.Remove(oldFeed);
                    Util.log("Remove RssFeed: " + oldFeed);
                }
            }
        }

        public async Task<List<Setting>> getSettings(){
            String resp = await client.GetStringAsync(hostAddress + ":" + port + GET_SETTINGS_ENTRY + "&token=" + token + "&t=" + Util.getTime());
            return Setting.fromJson(resp);                    
        }

        public async Task<TransferHistory> getTransferHistory() {
            TransferHistory hist = new TransferHistory();
            try
            {
                String resp = await client.GetStringAsync(hostAddress + ":" + port + GET_TRANS_HIST_ENTRY + "&token=" + token + "&t=" + Util.getTime());
                JObject jobj = JObject.Parse(resp);
                JToken hists = jobj.GetValue("transfer_history");
                if (hists != null)
                {
                    //................
                    return hist;
                }
            }
            catch (Exception e)
            {
                throw new UTException("Can not get transfer history info.",e);
            }
            return null;
        }

        public async Task<InitData> getInitData()
        {
            try
            {
                cacheID = "0";// reset global cacheID
                String resp = await client.GetStringAsync(hostAddress + ":" + port + GET_HEART_BEAT_ENTRY + "&cid=" + cacheID + "&token=" + token + "&t=" + Util.getTime());
                return InitData.fromJson(resp);
            }
            catch (Exception e)
            {
                throw new UTException("Can not get setting info.");
            }
        }

        public async Task<HeartBeatInfo> getHeartBeat() {
            try
            {
                String resp = await client.GetStringAsync(hostAddress + ":" + port + GET_HEART_BEAT_ENTRY + "&cid="+cacheID +"&token=" + token + "&t=" + Util.getTime());
                HeartBeatInfo info = HeartBeatInfo.fromJson(resp);
                cacheID = info.cacheID; // Update global cacheID
                return info;
            }
            catch (Exception e)
            {
                throw new UTException("Lose connection to uTorrent, please check your uTorrent client and login again.",e);
            }
        }

        // IList<object> is a runtime type, actually seen as List<Torrent>
        public async Task excuteAction(IList<object> targets, String action)
        {
            try
            {
                String request = hostAddress + ":" + port + GET_HEART_BEAT_ENTRY +"&action="+action;
                foreach(Torrent t in targets)
                    request += "&hash="+t.Hash;
                request += "&cid=" + cacheID + "&token=" + token + "&t=" + Util.getTime();
                String resp = await client.GetStringAsync(request);
                HeartBeatInfo info = HeartBeatInfo.fromJson(resp);
                cacheID = info.cacheID; // Update global cacheID
                
                UpdateInfo(info);
            }
            catch (Exception e)
            {
                throw new UTException("Excute action["+action+"] error.", e);
            }
        }

        public async void excuteAction(object target, String action) { 
            List<object> list = new List<object>();
            list.Add(target);
            await excuteAction(list, action);
        }

        public string getTorrentFileDownloadURL(Torrent t, int fileIndex){
            return hostAddress + ":" + port + GET_FILE_DOWNLOAD_ENTRY + "&sid="+t.StreamID +"&file=" + fileIndex;
        }

        public class Setting {
            private String _key, _num, _value, _access;
            public String key { get { return _key; } set { _key = value; RaisePropertyChanged(); } }
            public String num { get { return _num; } set { _num = value; RaisePropertyChanged(); } }
            public String value { get { return _value; } set { _value = value; RaisePropertyChanged(); } }
            public String access { get { return _access; } set { _access = value; RaisePropertyChanged(); } }

            public static List<Setting> fromJson(String json) {
                List<Setting> list = new List<Setting>();
                try
                {
                    JObject jobj = JObject.Parse(json);
                    JToken settings = jobj.GetValue("settings");
                    if (settings != null)
                    {
                        foreach (JToken set in settings)
                        {
                            Setting s = new Setting();
                            int index = 0;
                            foreach (JToken jt in set)
                            {
                                switch (index)
                                {
                                    case 0: s.key = jt.ToString();
                                        break;
                                    case 1: s.num = jt.ToString();
                                        break;
                                    case 2: s.value = jt.ToString();
                                        break;
                                    case 3: s.access = ((JProperty)jt.First).Value.ToString();
                                        break;
                                }
                                list.Add(s);
                                index++;
                            }
                        }
                        return list;
                    }
                }
                catch (Exception e)
                {
                    throw new UTException("Can not get setting info.",e);
                }
                return null;
            }

            private void RaisePropertyChanged([CallerMemberName] string caller = "")
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(caller));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public override string ToString()
            {
                return key + ","+num+","+value+","+access;
            }
        }

        public class TransferHistory { }

        // Torrent class
        // Implement INotifyPropertyChanged to notify data changes to UI Elements
        public class Torrent : INotifyPropertyChanged{
            private String _hash;
            public String Hash { get { return _hash; } set { _hash = value; RaisePropertyChanged(); } }
            private String _status;
            public String Status{ get{return _status;}set { _status = value; RaisePropertyChanged();}}
            private String _name;
            public String Name { get { return _name; } set { _name = value; RaisePropertyChanged(); } }
            private String _size;
            public String Size{ get { return _size; } set{_size = value;RaisePropertyChanged();}}
            private String _progress;
            public String Progress{ get{return _progress;} set { _progress = value; RaisePropertyChanged(); }}
            private String _downloaded;
            public String Downloaded { get { return _downloaded; } set { _downloaded = value; RaisePropertyChanged(); } }
            private String _uploaded;
            public String Uploaded { get { return _uploaded; } set { _uploaded = value; RaisePropertyChanged(); } }
            private String _ratio;
            public String Ratio { get { return _ratio; } set { _ratio = value; RaisePropertyChanged(); } }
            private String _upSpeed;
            public String UpSpeed { get { return _upSpeed; } set { _upSpeed = value; RaisePropertyChanged(); } }
            private String _downSpeed;
            public String DownSpeed { get { return _downSpeed; } set { _downSpeed = value; RaisePropertyChanged(); } }
            private String _ETA;
            public String ETA { get { return _ETA; } set { _ETA = value; RaisePropertyChanged(); } }
            private String _label;//only main label displayed here (if exists)
            public String Label { get { return _label; } set { _label = value; RaisePropertyChanged(); } }
            private String _peersConnected;
            public String PeersConnected { get { return _peersConnected; } set { _peersConnected = value; RaisePropertyChanged(); } }
            private String _peerSwarm;
            public String PeersSwarm { get { return _peerSwarm; } set { _peerSwarm = value; RaisePropertyChanged(); } }
            private String _seedsConnected;
            public String SeedsConnected { get { return _seedsConnected; } set { _seedsConnected = value; RaisePropertyChanged(); } }
            private String _seedsSwarm;
            public String SeedsSwarm { get { return _seedsSwarm; } set { _seedsSwarm = value; RaisePropertyChanged(); } }
            private String _availability;
            public String Availability { get { return _availability; } set { _availability = value; RaisePropertyChanged(); } }
            private String _queuePosition;
            public String QueuePosition { get { return _queuePosition; } set { _queuePosition = value; RaisePropertyChanged(); } }
            private String _remaining;
            public String Remaining { get { return _remaining; } set { _remaining = value; RaisePropertyChanged(); } }
            private String _downloadUrl;
            public String DownloadUrl { get { return _downloadUrl; } set { _downloadUrl = value; RaisePropertyChanged(); } }
            private String _rssFeedUrl;
            public String RssFeedUrl { get { return _rssFeedUrl; } set { _rssFeedUrl = value; RaisePropertyChanged(); } }
            private String _statusMessage;
            public String StatusMessage { get { return _statusMessage; } set { _statusMessage = value; RaisePropertyChanged(); } }
            private String _streamID;
            public String StreamID { get { return _streamID; } set { _streamID = value; RaisePropertyChanged(); } }
            private String _dateAdded;
            public String DateAdded { get { return _dateAdded; } set { _dateAdded = value; RaisePropertyChanged(); } }
            private String _dateCompleted;
            public String DateCompleted { get { return _dateCompleted; } set { _dateCompleted = value; RaisePropertyChanged(); } }
            private String _appUpdateUrl;
            public String AppUpdateUrl { get { return _appUpdateUrl; } set { _appUpdateUrl = value; RaisePropertyChanged(); } }
            private String _savePath;
            public String SavePath { get { return _savePath; } set { _savePath = value; RaisePropertyChanged(); } }

            // Additional properties to make UI conveniently
            private bool _toBeRemoved = false; // default to false
            public bool ToBeRemoved { get { return _toBeRemoved; } set { _toBeRemoved = value; RaisePropertyChanged(); } }

            public static Torrent fromJson(JToken torrentToken) {
                Torrent t = new Torrent();
                try
                {
                    int index = 0;
                    foreach (JToken token in torrentToken.Children()) {
                        String s = token.ToString();
                        switch (index) {
                            case 0: t.Hash = s; break;
                            case 1: t.Status = s; break;
                            case 2: t.Name = s; break;
                            case 3: t.Size = s; break;
                            case 4: t.Progress = s; break;
                            case 5: t.Downloaded = s; break;
                            case 6: t.Uploaded = s; break;
                            case 7: t.Ratio = s; break;
                            case 8: t.UpSpeed = s; break;
                            case 9: t.DownSpeed = s; break;
                            case 10: t.ETA = s; break;
                            case 11: t.Label = s; break;
                            case 12: t.PeersConnected = s; break;
                            case 13: t.PeersSwarm = s; break;
                            case 14: t.SeedsConnected = s; break;
                            case 15: t.SeedsSwarm = s; break;
                            case 16: t.Availability = s; break;
                            case 17: t.QueuePosition = s; break;
                            case 18: t.Remaining = s; break;
                            case 19: t.DownloadUrl = s; break;
                            case 20: t.RssFeedUrl = s; break;
                            case 21: t.StatusMessage = s; break;
                            case 22: t.StreamID = s; break;
                            case 23: t.DateAdded = s; break;
                            //case 24: t.AppUpdateUrl = s; break; // no good result
                            case 26: t.SavePath = s; break; // Not consistent with offical constant declaration
                        }
                        index++;
                    }
                    
                    return t;
                }
                catch (Exception e) {
                    throw new UTException("Error happeded when parsing torrent info.",e);
                }
                return null;
            }

            private void RaisePropertyChanged([CallerMemberName] string caller = "")
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(caller));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public override string ToString()
            {
                return Hash+","+Name+","+Size+","+DateAdded;
            }
        }

        public class RssFeed : INotifyPropertyChanged{
            private String _ID;
            public String ID { get { return _ID; } set { _ID = value; RaisePropertyChanged(); } }
            private String _enabled;
            public String Enabled { get { return _enabled; } set { _enabled = value; RaisePropertyChanged(); } }
            private String _useFeedTitle;
            public String UseFeedTitle { get { return _useFeedTitle; } set { _useFeedTitle = value; RaisePropertyChanged(); } }
            private String _userSelected;
            public String UserSelected { get { return _userSelected; } set { _userSelected = value; RaisePropertyChanged(); } }
            private String _programmed;
            public String Programmed { get { return _programmed; } set { _programmed = value; RaisePropertyChanged(); } }
            private String _downloadState;
            public String DownloadState { get { return _downloadState; } set { _downloadState = value; RaisePropertyChanged(); } }
            private String _URL;
            public String URL { get { return _URL; } set { 
                _URL = value; RaisePropertyChanged();
                int index = _URL.IndexOf('|');
                if (index > 0)
                    Name = _URL.Substring(0, index);
                else
                    Name = _URL;
            } }
            private String _nextUpdate;
            public String NextUpdate { get { return _nextUpdate; } set { _nextUpdate = value; RaisePropertyChanged(); } }
            private ObservableCollection<RssFeedItem> _items = new ObservableCollection<RssFeedItem>();
            public ObservableCollection<RssFeedItem> Items { get { return _items; } set {
                _items.Clear();
                foreach (RssFeedItem item in value) {
                    _items.Add(item); // simplified copy here, rss feed info should be static in short time
                }
            } }

            // Aditional Properties for UI
            private String _name;
            public String Name { get { return _name; } set { _name = value; RaisePropertyChanged(); } }
            private bool _toBeRemoved = false;
            public bool ToBeRemoved { get { return _toBeRemoved; } set { _toBeRemoved = value; RaisePropertyChanged(); } }

            public static RssFeed fromJson(JToken token) {
                RssFeed rf = new RssFeed();
                rf.ID = token.ElementAt(0).ToString();
                rf.Enabled = token.ElementAt(1).ToString();
                rf.UseFeedTitle = token.ElementAt(2).ToString();
                rf.UserSelected = token.ElementAt(3).ToString();
                rf.Programmed = token.ElementAt(4).ToString();
                rf.DownloadState = token.ElementAt(5).ToString();
                rf.URL = token.ElementAt(6).ToString();
                rf.NextUpdate = token.ElementAt(7).ToString();
                foreach (JToken t in token.ElementAt(8)) { 
                    rf.Items.Add(RssFeedItem.fromJson(t));
                }
                return rf;
            }

            private void RaisePropertyChanged([CallerMemberName] string caller = "")
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(caller));
                }
            }

            public override string ToString()
            {
                return "["+ this.ID + "] " + this.URL;
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        public class RssFeedItem: INotifyPropertyChanged{
            public static readonly List<String> RssItemCodecs = new List<string>() { 
                "?","MPEG","MPEG-2","MPEG-4","Real","WMV","Xvid","DivX","X264","H264","WMV-HD","VC1"
            };

            public static readonly List<String> RssItemQualities = new List<string>()
            {
                "?","HDTV","TVRip","DVDRip","SVCD","DSRip","DVBRip","PDTV","HR.HDTV","HR.PDTV","DVDR","DVDScr","720p","1080i","1080p","WebRip","SatRip"
            };

            private String _name;
            public String Name { get { return _name; } set { _name = value; RaisePropertyChanged(); } }
            private String _nameFull;
            public String NameFull { get { return _nameFull; } set{_nameFull = value; RaisePropertyChanged();}}
            private String _URL;
            public String URL { get { return _URL; } set { _URL = value; RaisePropertyChanged(); } }
            private String _quality;
            public String Quality { get { return _quality; } set {
                int i;
                if (int.TryParse(value, out i)) {
                    if (i == -1)
                        _quality = "All"; // should be very very rare...
                    else if (i == 0)
                        _quality = RssItemQualities[0];
                    else
                    {
                        int index = 1;
                        StringBuilder sb = new StringBuilder();
                        while (i > 0)
                        {
                            if ((i & 1) > 0) // test lowest bit
                                sb.Append("["+ RssItemQualities[index] + "]");
                            i = i >> 1; //right shift
                            index++;
                        }
                        _quality = sb.ToString();
                    }
                }else
                    _quality = value;
                RaisePropertyChanged(); 
            } }
            private String _codec;
            public String Codec { get { return _codec; } set {
                int index;
                if (int.TryParse(value, out index)) {
                    _codec = RssItemCodecs[index];
                }else
                    _codec = value;
                RaisePropertyChanged(); 
            } }
            private String _timeStamp;
            public String TimeStamp { get { return _timeStamp; } set { _timeStamp = value; RaisePropertyChanged(); } }
            private String _season;
            public String Season { get { return _season; } set { _season = value; RaisePropertyChanged(); } }
            private String _episode;
            public String Episode { get { return _episode; } set { _episode = value; RaisePropertyChanged(); } }
            private String _episodeTo;
            public String EpisodeTo { get { return _episodeTo; } set { _episodeTo = value; RaisePropertyChanged(); } }
            private String _repack;
            public String Repack { get { return _repack; } set { _repack = value; RaisePropertyChanged(); } }
            private String _inHistory;
            public String InHistory { get { return _inHistory; } set { _inHistory = value; RaisePropertyChanged(); } }

            public static RssFeedItem fromJson(JToken token) {
                RssFeedItem fi =  new RssFeedItem();
                fi.Name = token.ElementAt(0).ToString() ;
                fi.NameFull = token.ElementAt(1).ToString();
                fi.URL = token.ElementAt(2).ToString();
                fi.Quality = token.ElementAt(3).ToString();
                fi.Codec = token.ElementAt(4).ToString();
                fi.TimeStamp = token.ElementAt(5).ToString();
                fi.Season = token.ElementAt(6).ToString();
                fi.Episode = token.ElementAt(7).ToString();
                fi.EpisodeTo = token.ElementAt(8).ToString();
                fi.Repack = token.ElementAt(9).ToString();
                fi.InHistory = token.ElementAt(10).ToString();
                return fi;
            }

            private void RaisePropertyChanged([CallerMemberName] string caller = "")
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(caller));
                }
            }   
            public event PropertyChangedEventHandler PropertyChanged;
        }

        public class RssFilter {
            public static RssFilter fromJson(JToken token) {
                return new RssFilter();
            }
        }

        public class Message {
            public static Message fromJson(JToken token) {
                return new Message();
            }
        }

        public class Label {
            public static Label fromJson(JToken token) {
                return new Label();
            }
        }

        public class InitData {
            public String cacheID;
            public List<Torrent> torrents = new List<Torrent>();
            public List<Label> lables = new List<Label>();
            public List<RssFeed> rssfeeds = new List<RssFeed>();
            public List<RssFilter> rssFilters = new List<RssFilter>();
            public List<Message> messages = new List<Message>();

            public static InitData fromJson(String json) {
                InitData data = new InitData();
                try
                {
                    JObject jobj = JObject.Parse(json);
                    data.cacheID = jobj.GetValue("torrentc").ToString();
                    JToken ts = jobj.GetValue("torrents");
                    if(ts != null)
                    foreach (JToken t in ts) 
                        data.torrents.Add(Torrent.fromJson(t));
                    JToken ls = jobj.GetValue("labels");
                    if(ls != null)
                    foreach (JToken l in ls)
                        data.lables.Add(Label.fromJson(l));
                    JToken rfs = jobj.GetValue("rssfeeds");
                    if (rfs != null)
                    foreach(JToken rf in rfs)
                        data.rssfeeds.Add(RssFeed.fromJson(rf));
                    JToken rls = jobj.GetValue("rssfilters");
                    if (rls != null)
                    foreach(JToken rl in rls)
                        data.rssFilters.Add(RssFilter.fromJson(rl));
                    JToken msgs = jobj.GetValue("messages");
                    if (msgs != null)
                    foreach (JToken msg in msgs)
                        data.messages.Add(Message.fromJson(msg));
                    return data;
                }
                catch (Exception e) {
                    MessageDialog md = new MessageDialog("Error happeded when parsing heart-beat info package.");
                    md.ShowAsync();
                }
                return null;
            }
        }

        public async Task<List<File>> getFileList(Torrent t) {
            try
            {
                String resp = await client.GetStringAsync(hostAddress + ":" + port + GET_FILE_LIST_ENTRY + "&hash="+t.Hash+"&token=" + token + "&t=" + Util.getTime());
                return File.fromJson(resp);
            }
            catch (Exception e)
            {
                throw new UTException("Can not get file list of the selected torrent.", e);
            }
        }

        public class HeartBeatInfo {
            public String cacheID;

            public List<Torrent> torrentUpdated = new List<Torrent>();
            public List<String> torrentRemoved = new List<string>();

            public List<RssFeed> rssFeedUpdated = new List<RssFeed>();
            public List<String> rssFeedRemoved = new List<string>();

            public List<RssFilter> rssFilterUpdated = new List<RssFilter>();
            public List<String> rssFilterRemoved = new List<string>();

            public List<Message> messages = new List<Message>();

            public List<Label> labels = new List<Label>();

            public static HeartBeatInfo fromJson(String json) {
                HeartBeatInfo info = new HeartBeatInfo();
                try {
                    JObject jobj = JObject.Parse(json);
                    info.cacheID = jobj.GetValue("torrentc").ToString();
                    JToken tpList = jobj.GetValue("torrentp");
                    if (tpList != null)
                    foreach (JToken tp in tpList) { 
                        info.torrentUpdated.Add(Torrent.fromJson(tp));
                    }
                    JToken tmList = jobj.GetValue("torrentm");
                    if (tmList != null)
                    foreach (JToken tm in tmList)
                    {
                        info.torrentRemoved.Add(tm.ToString());
                    }
                    JToken rfpList = jobj.GetValue("rssfeedp");
                    if (rfpList != null)
                    foreach (JToken rfp in rfpList)
                    {
                        info.rssFeedUpdated.Add(RssFeed.fromJson(rfp));
                    }
                    JToken rfmList = jobj.GetValue("rssfeedm");
                    if (rfmList != null)
                    foreach (JToken rfm in rfmList)
                    {
                        info.rssFeedRemoved.Add(rfm.ToString());
                    } 
                    JToken rlpList = jobj.GetValue("rssfilterp");
                    if (rlpList != null) 
                        foreach (JToken rlp in rlpList)
                    {
                        info.rssFilterUpdated.Add(RssFilter.fromJson(rlp));
                    }
                    JToken rlmList = jobj.GetValue("rssfilterm");
                    if (rlmList != null)
                    foreach (JToken rlm in rlmList)
                    {
                        info.rssFilterRemoved.Add(rlm.ToString());
                    }
                    JToken msgList = jobj.GetValue("messages");
                    if (msgList != null)
                    foreach (JToken msg in msgList)
                    {
                        info.messages.Add(Message.fromJson(msg));
                    }
                    JToken labelList = jobj.GetValue("label");
                    if (labelList != null)
                        foreach (JToken label in labelList)
                        {
                            info.labels.Add(Label.fromJson(label));
                        }
                    return info;
                }
                catch (Exception e) {
                    throw new UTException("Error happeded when parsing heart-beat info package.",e);
                }
            }
        }

        public class File : INotifyPropertyChanged
        {
            public static readonly List<String> FilePrioritys = new List<String>{ "Skip", "Low", "Normal", "High"};

            private String _name;
            public String Name { get { return _name; } set { _name = value; RaisePropertyChanged(); } }
            private String _size;
            public String Size { get { return _size; } set { 
                _size = value;
                if (String.IsNullOrEmpty(value) || String.IsNullOrEmpty(Downloaded))
                    Progress = "0";
                else
                {
                    double v = double.Parse(value);
                    if (v <= 0)
                        Progress = "0";
                    else
                        Progress = (int)(double.Parse(Downloaded) / v * 1000) + "";
                }
                RaisePropertyChanged(); 
            } }
            private String _downloaded;
            public String Downloaded { get { return _downloaded; } set { 
                _downloaded = value;
                if (String.IsNullOrEmpty(value) || String.IsNullOrEmpty(Downloaded))
                    Progress = "0";
                else
                {
                    double v = double.Parse(value);
                    double t = double.Parse(Size);
                    if (t <= 0)
                        Progress = "0";
                    else
                        Progress = (int)(v / t * 1000) + "";
                }
                RaisePropertyChanged(); 
            } }
            private String _priority;
            public String Priority { get { return _priority; } set { 
                int index;
                if(int.TryParse(value, out index)){
                    _priority = FilePrioritys[index];
                }else
                    _priority = value;
                RaisePropertyChanged(); } }
            private String _firstPiece;
            public String FirstPiece { get { return _firstPiece; } set { _firstPiece = value; RaisePropertyChanged(); } }
            private String _numPieces;
            public String NumPieces { get { return _numPieces; } set { _numPieces = value; RaisePropertyChanged(); } }
            private String _streamable;
            public String Streamable { get { return _streamable; } set { _streamable = value; RaisePropertyChanged(); } }
            private String _encodedRate;
            public String EncodedRate { get { return _encodedRate; } set { _encodedRate = value; RaisePropertyChanged(); } }
            private String _duration;
            public String Duration { get { return _duration; } set { _duration = value; RaisePropertyChanged(); } }
            private String _width;
            public String Width { get { return _width; } set { _width = value; RaisePropertyChanged(); } }
            private String _height;
            public String Height { get { return _height; } set { _height = value; RaisePropertyChanged(); } }
            private String _streamETA;
            public String StreamETA { get { return _streamETA; } set { _streamETA = value; RaisePropertyChanged(); } }
            //private String _streamAbility;
            //public String StreamAbility { get { return _streamAbility; } set { _streamAbility = value; RaisePropertyChanged(); } }

            //Additional properties
            private String _progress;
            public String Progress { get { return _progress; } set { _progress = value; RaisePropertyChanged(); } }
            private int _index;
            public int Index { get { return _index; } set { _index = value; RaisePropertyChanged(); } }

            public static List<File> fromJson(String t) {
                List<File> list = new List<File>();
                JObject jobj = JObject.Parse(t);
                JToken files = jobj.GetValue("files").ElementAt(1);
                int index = 0;
                foreach (JToken ft in files) {
                    File f = new File();
                    f.Name = ft.ElementAt(0).ToString();
                    f.Size = ft.ElementAt(1).ToString();
                    f.Downloaded = ft.ElementAt(2).ToString();
                    f.Priority = ft.ElementAt(3).ToString();
                    f.FirstPiece = ft.ElementAt(4).ToString();
                    f.NumPieces = ft.ElementAt(5).ToString();
                    f.Streamable = ft.ElementAt(6).ToString();
                    f.EncodedRate = ft.ElementAt(7).ToString();
                    f.Duration = ft.ElementAt(8).ToString();
                    f.Width = ft.ElementAt(9).ToString();
                    f.Height = ft.ElementAt(10).ToString();
                    f.StreamETA = ft.ElementAt(11).ToString();
                    //f.StreamAbility = ft.ElementAt(12).ToString();
                    f.Index = index;
                    index++;
                    list.Add(f);
                }
                return list;
            }

            private void RaisePropertyChanged([CallerMemberName] string caller = "")
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(caller));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        public class UTException : Exception {
            public UTException() : base() { }
            public UTException(string message) : base(message) { }
            public UTException(string message, Exception innerException)
                :base(message,innerException){
            }
        }
    }
}
