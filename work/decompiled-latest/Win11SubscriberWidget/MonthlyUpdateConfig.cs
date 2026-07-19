namespace Win11SubscriberWidget;

public class MonthlyUpdateConfig
{
	public string channel_key { get; set; }

	public int year { get; set; }

	public int month { get; set; }

	public string detected_at { get; set; }

	public int video_count { get; set; }

	public int issued_makeup_cards { get; set; }

	public bool is_makeup { get; set; }
}
