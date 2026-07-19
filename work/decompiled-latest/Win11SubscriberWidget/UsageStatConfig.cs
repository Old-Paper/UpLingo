namespace Win11SubscriberWidget;

public class UsageStatConfig
{
	public string app_id { get; set; }

	public string label { get; set; }

	public long total_seconds { get; set; }

	public long today_seconds { get; set; }

	public string today_date { get; set; }

	public string last_used_at { get; set; }
}
