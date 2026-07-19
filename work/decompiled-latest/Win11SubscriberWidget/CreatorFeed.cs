using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Script.Serialization;
using System.Xml;

namespace Win11SubscriberWidget;

internal static class CreatorFeed
{
	public static VideoHistoryResult GetVideoHistory(string youtubeChannelId, string uploadsPlaylistId, string apiKey, int fullHistoryYear)
	{
		VideoHistoryResult playlistHistory = null;
		if (!string.IsNullOrWhiteSpace(uploadsPlaylistId) && !IsPlaceholder(apiKey))
		{
			DateTime cutoff = (fullHistoryYear > 0) ? new DateTime(fullHistoryYear, 1, 1) : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
			playlistHistory = TryGetPlaylistHistory(uploadsPlaylistId, apiKey, cutoff, fullHistoryYear);
			playlistHistory.FullHistoryAttempted = fullHistoryYear > 0;
			if (playlistHistory.Success)
			{
				return playlistHistory;
			}
		}
		VideoHistoryResult result = new VideoHistoryResult();
		if (TryGetVideoHistory(youtubeChannelId, result.PublishedTimes, out result.LatestAt, out result.LatestTitle, out result.LatestVideoId))
		{
			result.Success = true;
		}
		result.FullHistoryAttempted = fullHistoryYear > 0;
		result.HistoryTruncated = playlistHistory != null && playlistHistory.HistoryTruncated;
		return result;
	}

	public static bool TryGetVideoHistory(string youtubeChannelId, List<DateTime> publishedTimes, out DateTime latestAt, out string latestTitle, out string latestVideoId)
	{
		latestAt = DateTime.MinValue;
		latestTitle = "";
		latestVideoId = "";
		if (string.IsNullOrEmpty(youtubeChannelId) || !youtubeChannelId.StartsWith("UC"))
		{
			return false;
		}
		try
		{
			string xml = Download("https://www.youtube.com/feeds/videos.xml?channel_id=" + Uri.EscapeDataString(youtubeChannelId));
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(xml);
			XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
			xmlNamespaceManager.AddNamespace("a", "http://www.w3.org/2005/Atom");
			xmlNamespaceManager.AddNamespace("yt", "http://www.youtube.com/xml/schemas/2015");
			XmlNodeList xmlNodeList = xmlDocument.SelectNodes("/a:feed/a:entry", xmlNamespaceManager);
			if (xmlNodeList == null || xmlNodeList.Count == 0)
			{
				return false;
			}
			foreach (XmlNode item in xmlNodeList)
			{
				XmlNode xmlNode2 = item.SelectSingleNode("a:published", xmlNamespaceManager);
				if (xmlNode2 != null && DateTime.TryParse(xmlNode2.InnerText, out var result))
				{
					publishedTimes.Add(result);
					if (result > latestAt)
					{
						latestAt = result;
						XmlNode xmlNode3 = item.SelectSingleNode("a:title", xmlNamespaceManager);
						latestTitle = ((xmlNode3 == null) ? "" : xmlNode3.InnerText);
						XmlNode xmlNode4 = item.SelectSingleNode("yt:videoId", xmlNamespaceManager);
						latestVideoId = ((xmlNode4 == null) ? "" : xmlNode4.InnerText);
					}
				}
			}
			return latestAt != DateTime.MinValue;
		}
		catch (Exception ex)
		{
			AppLogger.Error("youtube-rss", ex);
			return false;
		}
	}

	public static bool TryGetLatestPublished(string youtubeChannelId, out DateTime latestAt)
	{
		List<DateTime> publishedTimes = new List<DateTime>();
		string latestTitle;
		string latestVideoId;
		return TryGetVideoHistory(youtubeChannelId, publishedTimes, out latestAt, out latestTitle, out latestVideoId);
	}

	private static string Download(string url)
	{
		using TimeoutWebClient timeoutWebClient = new TimeoutWebClient();
		timeoutWebClient.TimeoutMilliseconds = 10000;
		timeoutWebClient.Encoding = Encoding.UTF8;
		timeoutWebClient.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/125 Safari/537.36");
		return timeoutWebClient.DownloadString(url);
	}

	private static VideoHistoryResult TryGetPlaylistHistory(string playlistId, string apiKey, DateTime cutoff, int completeYear)
	{
		VideoHistoryResult result = new VideoHistoryResult();
		try
		{
			string pageToken = "";
			for (int page = 0; page < 20; page++)
			{
				string url = "https://www.googleapis.com/youtube/v3/playlistItems?part=snippet,contentDetails&maxResults=50&playlistId=" + Uri.EscapeDataString(playlistId) + "&key=" + Uri.EscapeDataString(apiKey);
				if (pageToken.Length > 0)
				{
					url += "&pageToken=" + Uri.EscapeDataString(pageToken);
				}
				Dictionary<string, object> root = ParseJsonObject(Download(url));
				if (root.ContainsKey("error") || !root.TryGetValue("items", out var itemsValue) || !(itemsValue is object[] items))
				{
					return result;
				}
				bool reachedCutoff = false;
				foreach (object itemValue in items)
				{
					Dictionary<string, object> item = itemValue as Dictionary<string, object>;
					Dictionary<string, object> snippet = GetObject(item, "snippet");
					Dictionary<string, object> contentDetails = GetObject(item, "contentDetails");
					string publishedText = ToStringValue(contentDetails, "videoPublishedAt");
					if (publishedText.Length == 0)
					{
						publishedText = ToStringValue(snippet, "publishedAt");
					}
					if (!DateTimeOffset.TryParse(publishedText, out var published))
					{
						continue;
					}
					DateTime publishedUtc = published.UtcDateTime;
					DateTime publishedLocal = published.LocalDateTime;
					if (publishedLocal < cutoff)
					{
						reachedCutoff = true;
						continue;
					}
					result.PublishedTimes.Add(publishedUtc);
					if (result.LatestAt == DateTime.MinValue || publishedUtc > result.LatestAt)
					{
						result.LatestAt = publishedUtc;
						result.LatestTitle = ToStringValue(snippet, "title");
						result.LatestVideoId = ToStringValue(contentDetails, "videoId");
						if (result.LatestVideoId.Length == 0)
						{
							result.LatestVideoId = ToStringValue(GetObject(snippet, "resourceId"), "videoId");
						}
					}
				}
				pageToken = ToStringValue(root, "nextPageToken");
				if (reachedCutoff || pageToken.Length == 0)
				{
					result.Success = true;
					result.CompleteYear = completeYear;
					return result;
				}
			}
			result.Success = result.PublishedTimes.Count > 0;
			result.HistoryTruncated = completeYear > 0 && pageToken.Length > 0;
		}
		catch (Exception ex)
		{
			AppLogger.Error("youtube-history", ex);
		}
		return result;
	}

	private static Dictionary<string, object> ParseJsonObject(string json)
	{
		return new JavaScriptSerializer().DeserializeObject(json) as Dictionary<string, object> ?? new Dictionary<string, object>();
	}

	private static Dictionary<string, object> GetObject(Dictionary<string, object> parent, string key)
	{
		if (parent != null && parent.TryGetValue(key, out var value) && value is Dictionary<string, object> dictionary)
		{
			return dictionary;
		}
		return new Dictionary<string, object>();
	}

	private static string ToStringValue(Dictionary<string, object> root, string key)
	{
		if (root != null && root.TryGetValue(key, out var value) && value != null)
		{
			return value.ToString();
		}
		return "";
	}

	private static bool IsPlaceholder(string value)
	{
		return string.IsNullOrWhiteSpace(value) || value.Trim().StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase);
	}
}
