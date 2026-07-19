using System;
using System.Collections.Generic;

namespace Win11SubscriberWidget;

internal sealed class VideoHistoryResult
{
	public bool Success;

	public DateTime LatestAt;

	public string LatestTitle = "";

	public string LatestVideoId = "";

	public List<DateTime> PublishedTimes = new List<DateTime>();

	public int CompleteYear;

	public bool FullHistoryAttempted;

	public bool HistoryTruncated;
}
