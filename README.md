uTorrentMetro
=============

![App logo](https://raw.github.com/tjumyk/uTorrentMetro/master/utorrentMetro/Assets/icon414x180.png)

A win8 Metro client for uTorrent.

Features
------------------------

* Connect to uTorrent Web API
* Fetch and monitor Torrent list
* Fetch and monitor Torrent file list
* Fetch and monitor Rss feed list
* Execute basic commands: start/pause/stop/remove Torrent
* Download file by external browser

ScreenShots
------------------------
![s1](https://raw.github.com/tjumyk/uTorrentMetro/master/utorrentMetro/screenshots/1.png)  
![s2](https://raw.github.com/tjumyk/uTorrentMetro/master/utorrentMetro/screenshots/2.png)  
![s3](https://raw.github.com/tjumyk/uTorrentMetro/master/utorrentMetro/screenshots/3.png)  
![s4](https://raw.github.com/tjumyk/uTorrentMetro/master/utorrentMetro/screenshots/4.png)  

Future Work
------------------------

* Implement GridView Filter to dynamiclly show Torrents in a certain category
* Add Torrent by seed file / seed url / rss feed
* Handle transfer history info
* Download file in app, keep track of download progress and download queue
* Show Toast when certain event happens(e.g. Torrent download finished, error occurs)
* Lookup and update uTorrent settings
* Preview files(text/media) using Http requests with authority header

Privacy
--------------------
uTorrentMetro use your internet connection to communicate with Web API provided by your own local uTorrent client (For example, fetching torrent list, sending start/pause command, etc.). So, no external network connection is used in this application and none of your personal information could be collected or transmitted.
