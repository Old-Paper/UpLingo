using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Win11SubscriberWidget;

internal static class RefreshService
{
	private const int MaxParallelRequests = 4;

	public static RefreshPayload Fetch(WidgetConfig snapshot, Func<bool> isCancelled)
	{
		int count = snapshot?.channels?.Count ?? 0;
		FetchResult[] array = new FetchResult[count];
		Parallel.For(0, count, new ParallelOptions
		{
			MaxDegreeOfParallelism = MaxParallelRequests
		}, delegate(int index)
		{
			if (isCancelled != null && isCancelled())
			{
				array[index] = new FetchResult { Ok = false, Error = "刷新已取消" };
				return;
			}
			ChannelConfig channel = snapshot.channels[index];
			try
			{
				array[index] = SubscriberFetcher.FetchDetails(channel, snapshot);
			}
			catch (Exception ex)
			{
				CachedCountConfig cached = ChannelIdentity.FindCachedCount(snapshot, channel);
				array[index] = new FetchResult
				{
					Ok = false,
					Error = ex.Message,
					HasCached = cached != null,
					CachedCount = cached?.count ?? 0L,
					CachedAt = cached?.updated_at
				};
				AppLogger.Error("channel-refresh", ex);
			}
		});

		List<FetchResult> results = new List<FetchResult>(array);
		CreatorFetchData creator = FetchCreator(snapshot, results, isCancelled);
		return new RefreshPayload
		{
			Results = results,
			Creator = creator
		};
	}

	private static CreatorFetchData FetchCreator(WidgetConfig snapshot, List<FetchResult> results, Func<bool> isCancelled)
	{
		CreatorFetchData creator = new CreatorFetchData();
		for (int i = 0; i < snapshot.channels.Count && i < results.Count; i++)
		{
			if (isCancelled != null && isCancelled())
			{
				return creator;
			}
			ChannelConfig channel = snapshot.channels[i];
			FetchResult result = results[i];
			if (channel == null || channel.benchmark || ChannelIdentity.IsBilibili(channel.platform) || result == null || !result.Ok || string.IsNullOrEmpty(result.YoutubeChannelId))
			{
				continue;
			}
			int currentYear = DateTime.Now.Year;
			CreatorStateConfig state = snapshot.creator_state;
			bool needFullHistory = NeedsFullHistory(state, result.YoutubeChannelId, DateTime.Now);
			string apiKey = ChannelIdentity.First(channel.youtube_api_key, channel.api_key, snapshot.youtube_api_key);
			VideoHistoryResult history = CreatorFeed.GetVideoHistory(result.YoutubeChannelId, result.YoutubeUploadsPlaylistId, apiKey, needFullHistory ? currentYear : 0);
			creator.ChannelKey = result.YoutubeChannelId;
			creator.VideoTimes = history.PublishedTimes;
			creator.CompleteHistoryYear = history.CompleteYear;
			creator.FullHistoryAttempted = history.FullHistoryAttempted;
			creator.HistoryTruncated = history.HistoryTruncated;
			if (history.Success && history.LatestAt != DateTime.MinValue)
			{
				creator.LatestVideoAt = history.LatestAt;
				creator.LatestVideoTitle = history.LatestTitle;
				creator.LatestVideoId = history.LatestVideoId;
			}
			break;
		}

		ConcurrentDictionary<int, DateTime> benchmarkDates = new ConcurrentDictionary<int, DateTime>();
		Parallel.For(0, snapshot.channels.Count, new ParallelOptions
		{
			MaxDegreeOfParallelism = MaxParallelRequests
		}, delegate(int index)
		{
			if (isCancelled != null && isCancelled())
			{
				return;
			}
			ChannelConfig channel = snapshot.channels[index];
			FetchResult result = (index < results.Count) ? results[index] : null;
			if (channel != null && channel.benchmark && !ChannelIdentity.IsBilibili(channel.platform) && result != null && result.Ok && !string.IsNullOrEmpty(result.YoutubeChannelId) && CreatorFeed.TryGetLatestPublished(result.YoutubeChannelId, out var latestAt))
			{
				benchmarkDates[index] = latestAt;
			}
		});
		foreach (KeyValuePair<int, DateTime> item in benchmarkDates)
		{
			creator.BenchmarkLatestVideo[item.Key] = item.Value;
		}
		return creator;
	}

	internal static bool NeedsFullHistory(CreatorStateConfig state, string channelId, DateTime localNow)
	{
		if (state == null || state.monthly_history_year != localNow.Year || !string.Equals(state.channel_key, channelId, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (state.monthly_history_complete)
		{
			return false;
		}
		return !DateTime.TryParse(state.monthly_history_retry_after, out var retryAfter) || retryAfter <= localNow;
	}
}
