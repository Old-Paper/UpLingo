using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Script.Serialization;

namespace Win11SubscriberWidget;

internal static class SubscriberFetcher
{
	public static FetchResult FetchDetails(ChannelConfig channel, WidgetConfig config)
	{
		switch ((channel.platform ?? "").Trim().ToLowerInvariant())
		{
		case "bilibili":
		case "bili":
		case "b站":
			return new FetchResult
			{
				Ok = true,
				Count = FetchBilibili(channel)
			};
		case "youtube":
		case "yt":
		case "油管":
			return FetchYouTube(channel, config);
		default:
			throw new Exception("不支持的平台：" + channel.platform);
		}
	}

	private static long FetchBilibili(ChannelConfig channel)
	{
		string text = First(channel.bilibili_uid, channel.uid, channel.vmid);
		if (IsPlaceholder(text))
		{
			throw new Exception("请填写 B 站 UID");
		}
		for (int i = 0; i < text.Length; i++)
		{
			if (!char.IsDigit(text[i]))
			{
				throw new Exception("B 站 UID 应该是数字");
			}
		}
		Dictionary<string, object> dictionary = ParseJsonObject(DownloadString("https://api.bilibili.com/x/relation/stat?vmid=" + Uri.EscapeDataString(text), "https://space.bilibili.com/" + text + "/"));
		if (ToInt(dictionary, "code") != 0)
		{
			throw new Exception("B 站接口异常：" + ToStringValue(dictionary, "message"));
		}
		Dictionary<string, object> dictionary2 = GetObject(dictionary, "data");
		if (!dictionary2.ContainsKey("follower"))
		{
			throw new Exception("B 站没有返回粉丝数");
		}
		return Convert.ToInt64(dictionary2["follower"]);
	}

	private static FetchResult FetchYouTube(ChannelConfig channel, WidgetConfig config)
	{
		string text = First(channel.youtube_api_key, channel.api_key, config.youtube_api_key);
		if (IsPlaceholder(text))
		{
			throw new Exception("请填写 YouTube API key");
		}
		KeyValuePair<string, string> keyValuePair = YouTubeQuery(channel);
		Dictionary<string, object> dictionary = ParseJsonObject(DownloadString("https://www.googleapis.com/youtube/v3/channels?part=snippet,statistics,contentDetails&key=" + Uri.EscapeDataString(text) + "&" + Uri.EscapeDataString(keyValuePair.Key) + "=" + Uri.EscapeDataString(keyValuePair.Value), null));
		if (dictionary.ContainsKey("error"))
		{
			throw new Exception(ToStringValue(GetObject(dictionary, "error"), "message"));
		}
		if (!dictionary.TryGetValue("items", out var value))
		{
			throw new Exception("YouTube 没有返回频道信息");
		}
		if (!(value is object[] array) || array.Length == 0)
		{
			throw new Exception("YouTube 没有找到这个频道");
		}
		Dictionary<string, object> dictionary2 = array[0] as Dictionary<string, object>;
		string youtubeChannelId = ToStringValue(dictionary2, "id");
		Dictionary<string, object> dictionary3 = GetObject(dictionary2, "statistics");
		if (dictionary3.ContainsKey("hiddenSubscriberCount") && Convert.ToBoolean(dictionary3["hiddenSubscriberCount"]))
		{
			throw new Exception("频道隐藏了订阅数");
		}
		if (!dictionary3.ContainsKey("subscriberCount"))
		{
			throw new Exception("YouTube 没有返回订阅数");
		}
		Dictionary<string, object> contentDetails = GetObject(dictionary2, "contentDetails");
		Dictionary<string, object> relatedPlaylists = GetObject(contentDetails, "relatedPlaylists");
		return new FetchResult
		{
			Ok = true,
			Count = Convert.ToInt64(dictionary3["subscriberCount"]),
			YoutubeChannelId = youtubeChannelId,
			YoutubeUploadsPlaylistId = ToStringValue(relatedPlaylists, "uploads")
		};
	}

	private static KeyValuePair<string, string> YouTubeQuery(ChannelConfig channel)
	{
		string value = First(channel.youtube_channel_id, channel.channel_id);
		if (!IsPlaceholder(value))
		{
			return new KeyValuePair<string, string>("id", value);
		}
		string text = First(channel.youtube_handle, channel.handle);
		if (!IsPlaceholder(text))
		{
			if (!text.StartsWith("@"))
			{
				text = "@" + text;
			}
			return new KeyValuePair<string, string>("forHandle", text);
		}
		string value2 = First(channel.youtube_username, channel.username);
		if (!IsPlaceholder(value2))
		{
			return new KeyValuePair<string, string>("forUsername", value2);
		}
		string text2 = First(channel.youtube_channel, channel.youtube_url, channel.url);
		if (IsPlaceholder(text2))
		{
			throw new Exception("请填写 YouTube 频道");
		}
		if (text2.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || text2.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
		{
			string[] array = new Uri(text2).AbsolutePath.Split(new char[1] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			if (array.Length != 0)
			{
				if (array[0].StartsWith("@"))
				{
					return new KeyValuePair<string, string>("forHandle", array[0]);
				}
				if (array[0] == "channel" && array.Length > 1)
				{
					return new KeyValuePair<string, string>("id", array[1]);
				}
				if (array[0] == "user" && array.Length > 1)
				{
					return new KeyValuePair<string, string>("forUsername", array[1]);
				}
			}
			throw new Exception("YouTube 链接格式不支持");
		}
		if (text2.StartsWith("@"))
		{
			return new KeyValuePair<string, string>("forHandle", text2);
		}
		if (text2.StartsWith("UC"))
		{
			return new KeyValuePair<string, string>("id", text2);
		}
		return new KeyValuePair<string, string>("forHandle", "@" + text2);
	}

	private static string DownloadString(string url, string referer)
	{
		using TimeoutWebClient timeoutWebClient = new TimeoutWebClient();
		timeoutWebClient.TimeoutMilliseconds = 10000;
		timeoutWebClient.Encoding = Encoding.UTF8;
		timeoutWebClient.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/125 Safari/537.36");
		timeoutWebClient.Headers.Add("Accept", "application/json,text/plain,*/*");
		if (!string.IsNullOrEmpty(referer))
		{
			timeoutWebClient.Headers.Add("Referer", referer);
		}
		return timeoutWebClient.DownloadString(url);
	}

	private static Dictionary<string, object> ParseJsonObject(string json)
	{
		return (new JavaScriptSerializer().DeserializeObject(json) as Dictionary<string, object>) ?? throw new Exception("接口返回内容不是 JSON 对象");
	}

	private static Dictionary<string, object> GetObject(Dictionary<string, object> parent, string key)
	{
		if (parent == null || !parent.TryGetValue(key, out var value))
		{
			return new Dictionary<string, object>();
		}
		return (value as Dictionary<string, object>) ?? new Dictionary<string, object>();
	}

	private static int ToInt(Dictionary<string, object> root, string key)
	{
		if (!root.TryGetValue(key, out var value))
		{
			return 0;
		}
		return Convert.ToInt32(value);
	}

	private static string ToStringValue(Dictionary<string, object> root, string key)
	{
		if (root == null || !root.TryGetValue(key, out var value) || value == null)
		{
			return "";
		}
		return value.ToString();
	}

	private static string First(params string[] values)
	{
		for (int i = 0; i < values.Length; i++)
		{
			if (!string.IsNullOrEmpty(values[i]))
			{
				return values[i].Trim();
			}
		}
		return "";
	}

	private static bool IsPlaceholder(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return true;
		}
		string text = value.Trim();
		if (!text.StartsWith("YOUR_") && !(text == "@your_handle_or_UC_channel_id"))
		{
			return text == "your_handle_or_UC_channel_id";
		}
		return true;
	}
}
