using System;
using System.Collections.Generic;

namespace Win11SubscriberWidget;

internal class CreatorFetchData
{
	public DateTime? LatestVideoAt;

	public string LatestVideoTitle;

	public string LatestVideoId;

	public string ChannelKey;

	public List<DateTime> VideoTimes;

	public int CompleteHistoryYear;

	public bool FullHistoryAttempted;

	public bool HistoryTruncated;

	public Dictionary<int, DateTime> BenchmarkLatestVideo = new Dictionary<int, DateTime>();
}
