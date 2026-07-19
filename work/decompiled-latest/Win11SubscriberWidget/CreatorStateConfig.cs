namespace Win11SubscriberWidget;

public class CreatorStateConfig
{
	public string channel_key { get; set; }

	public string last_video_at { get; set; }

	public string last_video_title { get; set; }

	public string checked_at { get; set; }

	public string configured_channel_key { get; set; }

	public int monthly_history_year { get; set; }

	public bool monthly_history_complete { get; set; }

	public string monthly_history_retry_after { get; set; }

	// Turns on only after the first post-history baseline is saved, so old videos never mint cards on upgrade.
	public bool makeup_rewards_armed { get; set; }
}
